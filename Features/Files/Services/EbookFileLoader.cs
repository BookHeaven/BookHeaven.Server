using BookHeaven.Domain;
using BookHeaven.Domain.Enums;
using BookHeaven.Domain.Features.Authors;
using BookHeaven.Domain.Features.Books;
using BookHeaven.Domain.Features.BookSeries;
using BookHeaven.Domain.Helpers;
using BookHeaven.EbookManager;
using BookHeaven.EbookManager.Enums;
using BookHeaven.Server.Features.Files.Abstractions;
using Microsoft.AspNetCore.Components.Forms;

namespace BookHeaven.Server.Features.Files.Services;

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
		if (!DomainGlobals.SupportedFormats.Contains(extension))
		{
			logger.LogWarning("Unsupported file extension: {Extension}", extension);
			return null;
		}
		
		var fileHash = await FileHelpers.GetPartialMd5HashAsync(path);
		if(string.IsNullOrEmpty(fileHash))
		{
			logger.LogError("Failed to compute file hash for book");
			return null;
		}
		
		var getBook = await sender.Send(new GetBook.Query { Hash = fileHash });
		if (getBook.IsSuccess)
		{
			logger.LogWarning("Book '{Path}' already exists in the database, ignoring", path);
			return null;
		}
		
		var ebookReader = ebookManagerProvider.GetReader((Format)EnumExtensions.GetFormatByExtension(extension));
			
		var ebook = await ebookReader.ReadMetadataAsync(path);
		
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
			Format = (EbookFormat)ebook.Format,
			FileHash = fileHash
		};
		
		var tempCoverPath = await GenerateCoverFile(ebook.Cover);
		
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

	private static async Task<string> GenerateCoverFile(byte[]? image)
	{
		if (image == null) return string.Empty;
		var dest = Path.GetTempFileName();
		var dir = Path.GetDirectoryName(dest);
		if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
		await File.WriteAllBytesAsync(dest, image);
		return dest;
	}
}