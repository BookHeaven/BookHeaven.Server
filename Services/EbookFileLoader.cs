using BookHeaven.Domain;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Enums;
using BookHeaven.Domain.Extensions;
using Microsoft.AspNetCore.Components.Forms;
using BookHeaven.Domain.Features.Authors;
using BookHeaven.Domain.Features.Books;
using BookHeaven.Domain.Features.BookSeries;
using BookHeaven.EbookManager;
using BookHeaven.EbookManager.Abstractions;
using BookHeaven.EbookManager.Enums;
using BookHeaven.Server.Abstractions;
using MediatR;

namespace BookHeaven.Server.Services;

public class EbookFileLoader(
	EbookManagerProvider ebookManagerProvider,
	ISender sender, 
	ILogger<EbookFileLoader> logger)
	: IEbookFileLoader
{
	
	public async Task<Guid?> LoadFromFile(IBrowserFile file)
	{
		Guid? id;
		try
		{
			var tempPath = Path.GetTempFileName() + Path.GetExtension(file.Name);
			await using (var fileStream = File.Create(tempPath))
			{
				await file.OpenReadStream(maxAllowedSize: 300 * 1024 * 1024).CopyToAsync(fileStream);
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
		
		var extension = Path.GetExtension(path).ToLowerInvariant();
		if (!Globals.SupportedFormats.Contains(extension))
		{
			logger.LogWarning("Unsupported file extension: {Extension}", extension);
			return null;
		}
		
		var ebookReader = ebookManagerProvider.GetReader((Format)EnumExtensions.GetFormatByExtension(extension));
			
		var ebook = await ebookReader.ReadMetadataAsync(path);
			
		var getBook = await sender.Send(new GetBook.Query(null, ebook.Title));
			
		if (getBook.IsSuccess)
		{
			logger.LogWarning("Book with title '{Title}' already exists in the database, ignoring", ebook.Title);
			return null;
		}
			
		var getAuthor = await sender.Send(new GetAuthor.Query(new GetAuthor.Filter {Name = ebook.Author}));
		if (getAuthor.IsFailure)
		{
			var createAuthor = await sender.Send(new CreateAuthor.Command(ebook.Author));
			if (createAuthor.IsFailure)
			{
				logger.LogError("Failed to create author '{Author}': {Description}", ebook.Author, createAuthor.Error.Description);
				return null;
			}
			authorId = createAuthor.Value.AuthorId;
		}
		else
		{
			authorId = getAuthor.Value.AuthorId;
		}
			
		if (!string.IsNullOrWhiteSpace(ebook.Series))
		{
			var getSeries = await sender.Send(new GetSeries.Query(null, ebook.Series));
			if (getSeries.IsFailure)
			{
				var createSeries = await sender.Send(new CreateSeries.Command(ebook.Series));
				if (createSeries.IsFailure)
				{
					logger.LogError("Failed to create series '{Series}': {Description}", ebook.Series, createSeries.Error.Description);
					return null;
				}
				seriesId = createSeries.Value.SeriesId;
			}
			else
			{
				seriesId = getSeries.Value.SeriesId;
			}
		}
		var isbnIdentifiers = ebook.Identifiers.Where(x => x.Scheme == "ISBN").ToList();
			
		var newBook = new Book
		{
			Title = ebook.Title,
			Description = ebook.Synopsis,
			PublishedDate = ebook.PublishDate != null && DateTime.TryParse(ebook.PublishDate, out var pubDate) ? pubDate : null,
			Publisher = ebook.Publisher,
			Language = ebook.Language,
			AuthorId = authorId,
			SeriesId = seriesId,
			SeriesIndex = ebook.SeriesIndex,
			ISBN10 = isbnIdentifiers.FirstOrDefault(x => x.Value.Length == 10)?.Value.Split(":").Last(),
			ISBN13 = isbnIdentifiers.FirstOrDefault(x => x.Value.Length == 13)?.Value.Split(":").Last(),
			ASIN = ebook.Identifiers.FirstOrDefault(x => x.Scheme == "ASIN")?.Value.Split(":").Last(),
			UUID = ebook.Identifiers.FirstOrDefault(x => x.Scheme == "UUID")?.Value.Split(":").Last(),
			Format = (EbookFormat)ebook.Format
		};
		
		var tempCoverPath = Path.GetTempFileName();
		await StoreCover(ebook.Cover, tempCoverPath);
		
		var createBook = await sender.Send(new AddBook.Command(newBook, tempCoverPath, path));
		if (createBook.IsFailure)
		{
			return null;
		}
		
		// Cleanup temp cover file
		if (File.Exists(tempCoverPath))
		{
			File.Delete(tempCoverPath);
		}
			
		return createBook.Value;
	}

	private static async Task StoreCover(byte[]? image, string dest)
	{
		if (image == null) return;
		var dir = Path.GetDirectoryName(dest);
		if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
		await File.WriteAllBytesAsync(dest, image);
	}
}