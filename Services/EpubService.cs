using EpubManager;
using EpubManager.Entities;
using Microsoft.AspNetCore.Components.Forms;
using BookHeaven.Server.Interfaces;
using System.Globalization;
using BookHeaven.Domain.Helpers;
using BookHeaven.Server.Features.Authors;
using BookHeaven.Server.Features.Books;
using BookHeaven.Server.Features.Seriess;
using MediatR;

namespace BookHeaven.Server.Services
{
	public class EpubService(IEpubReader epubReader, ISender sender, ILogger<EpubService> logger) : IFormatService<EpubBook>
	{
		public async Task<EpubBook> GetMetadata(string path)
		{
			return await epubReader.ReadAsync(path, true);
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
			
			var getBook = await sender.Send(new GetBookQuery(null, epubBook.Metadata.Title));
			
			if (getBook.IsSuccess)
			{
				return getBook.Value.BookId;
			}
			
			var getAuthor = await sender.Send(new GetAuthorQuery(null, epubBook.Metadata.Author));
			if (getAuthor.IsFailure)
			{
				var createAuthor = await sender.Send(new CreateAuthorCommand(epubBook.Metadata.Author));
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
				var getSeries = await sender.Send(new GetSeriesQuery(null, epubBook.Metadata.Series));
				if (getSeries.IsFailure)
				{
					var createSeries = await sender.Send(new CreateSeriesCommand(epubBook.Metadata.Series));
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
				new CreateBookCommand(
					AuthorId: authorId!.Value,
					SeriesId: seriesId,
					SeriesIndex: epubBook.Metadata.SeriesIndex,
					Title: epubBook.Metadata.Title,
					Description: epubBook.Metadata.Description,
					PublishedDate: DateTime.TryParseExact(epubBook.Metadata.PublishDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var tempDate) ? tempDate : DateTime.MinValue,
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
			
			await StoreCover(epubBook.Cover, Helpers.GetCoverPath(Program.CoversPath, createBook.Value)!);
			await StoreBook(epubBook.FilePath, Helpers.GetBookPath(Program.BooksPath, createBook.Value)!);
			
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
			await File.WriteAllBytesAsync(dest, image);
		}

		public async Task StoreBook(string? sourcePath, string dest)
		{
			if (sourcePath == null) return;
            await using FileStream source = File.OpenRead(sourcePath);
            await using FileStream destination = File.Create(dest);
            await source.CopyToAsync(destination);
        }
	}
}
