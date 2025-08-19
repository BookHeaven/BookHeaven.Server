using BookHeaven.EpubManager;
using Microsoft.AspNetCore.Components.Forms;
using BookHeaven.Server.Interfaces;
using System.Globalization;
using BookHeaven.Domain.Features.Authors;
using BookHeaven.Domain.Features.Books;
using BookHeaven.Domain.Features.BooksProgress;
using BookHeaven.Domain.Features.Profiles;
using BookHeaven.Domain.Features.Seriess;
using BookHeaven.Domain.Helpers;
using BookHeaven.EpubManager.Epub.Entities;
using BookHeaven.EpubManager.Epub.Services;
using MediatR;

namespace BookHeaven.Server.Services
{
	public class EpubService(IEpubReader epubReader, ISender sender, ILogger<EpubService> logger) : IFormatService<EpubBook>
	{
		public async Task<EpubBook> GetMetadata(string path)
		{
			return await epubReader.ReadMetadataAsync(path);
		}

		public async Task<Guid?> LoadFromFile(IBrowserFile file)
		{
			Guid? id;
			try
			{
				string tempPath = Path.GetTempFileName();
				await using (FileStream fileStream = File.Create(tempPath))
				{
					await file.OpenReadStream(maxAllowedSize: 1024 * 30000).CopyToAsync(fileStream);
				}

				id = await LoadFromFilePath(tempPath);
				File.Delete(tempPath);
			}
			catch (Exception e)
            {
				logger.LogError(e, "Failed to load book from file");
				id = null;
            }
			
			return id;
		}

		public async Task<Guid?> LoadFromFilePath(string path)
		{
			Guid? authorId = null;
			Guid? seriesId = null;
			
			
			var epubBook = await GetMetadata(path);
			// Book? book = await databaseService.GetBy<Book>(x => x.Title!.Equals(epubBook.Metadata.Title));
			
			var getBook = await sender.Send(new GetBook.Query(null, epubBook.Metadata.Title));
			
			if (getBook.IsSuccess)
			{
				return getBook.Value.BookId;
			}
			
			var getAuthor = await sender.Send(new GetAuthor.Query(new GetAuthor.Filter {Name = epubBook.Metadata.Author}));
			if (getAuthor.IsFailure)
			{
				var createAuthor = await sender.Send(new CreateAuthor.Command(epubBook.Metadata.Author));
				if (createAuthor.IsFailure)
				{
					return null;
				}
				authorId = createAuthor.Value.AuthorId;
			}
			else
			{
				authorId = getAuthor.Value.AuthorId;
			}
			
			if (epubBook.Metadata.Series != null)
			{
				var getSeries = await sender.Send(new GetSeries.Query(null, epubBook.Metadata.Series));
				if (getSeries.IsFailure)
				{
					var createSeries = await sender.Send(new CreateSeries.Command(epubBook.Metadata.Series));
					if (createSeries.IsFailure)
					{
						return null;
					}
					seriesId = createSeries.Value.SeriesId;
				}
				else
				{
					seriesId = getSeries.Value.SeriesId;
				}
			}
			var isbnIdentifiers = epubBook.Metadata.Identifiers.Where(x => x.Scheme == "ISBN").ToList();
			
			var createBook = await sender.Send(
				new CreateBook.Command(
					AuthorId: authorId!.Value,
					SeriesId: seriesId,
					SeriesIndex: epubBook.Metadata.SeriesIndex,
					Title: epubBook.Metadata.Title,
					Description: epubBook.Metadata.Description,
					PublishedDate: DateTime.TryParse(epubBook.Metadata.PublishDate, out var tempDate) ? tempDate : DateTime.MinValue,
					Publisher: epubBook.Metadata.Publisher,
					Language: epubBook.Metadata.Language,
					Isbn10: isbnIdentifiers.FirstOrDefault(x => x.Value.Length == 10)?.Value.Split(":").Last(),
					Isbn13: isbnIdentifiers.FirstOrDefault(x => x.Value.Length == 13)?.Value.Split(":").Last(),
					Asin: epubBook.Metadata.Identifiers.FirstOrDefault(x => x.Scheme == "ASIN")?.Value.Split(":").Last(),
					Uuid: epubBook.Metadata.Identifiers.FirstOrDefault(x => x.Scheme == "UUID")?.Value.Split(":").Last()
				)
			);
			
			if (createBook.IsFailure)
			{
				return null;
			}
			
			var getProfiles = await sender.Send(new GetAllProfiles.Query());
			if (getProfiles.IsFailure)
			{
				throw new Exception("Failed to get profiles");
			}

			foreach (var profile in getProfiles.Value)
			{
				await sender.Send(new CreateBookProgress.Command(createBook.Value, profile.ProfileId));
			}
			
			await StoreCover(epubBook.Cover, GetCoverPath(Program.CoversPath, createBook.Value)!);
			await StoreBook(epubBook.FilePath, GetBookPath(Program.BooksPath, createBook.Value)!);
			
			return createBook.Value;
		}

		public async Task LoadFromFolder(string path)
		{
			foreach (string file in Directory.EnumerateFiles(path, "*.epub", SearchOption.AllDirectories))
			{
				await LoadFromFilePath(file);
			}
		}

		public async Task StoreCover(byte[]? image, string dest)
		{
			if (image == null) return;
			var dir = Path.GetDirectoryName(dest);
			if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
			await File.WriteAllBytesAsync(dest, image);
		}

		public async Task StoreBook(string? sourcePath, string dest)
		{
			if (sourcePath == null) return;
			var dir = Path.GetDirectoryName(dest);
			if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
			await using var source = File.OpenRead(sourcePath);
			await using var destination = File.Create(dest);
			await source.CopyToAsync(destination);
		}
		
		public static string? GetBookPath(string booksPath, Guid bookId, bool checkPath = false)
		{
			var path = $"{booksPath}/{bookId}.epub";
			if (checkPath && !File.Exists(path))
			{
				return null;
			}
			return path;
		}
	
		public static string? GetCoverPath(string coversPath, Guid bookId, bool checkPath = false)
		{
			var path = $"{coversPath}/{bookId}.jpg";
			if (checkPath && !File.Exists(path))
			{
				return null;
			}
			return path;
		}
	}
}
