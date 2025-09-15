using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Events;
using Microsoft.AspNetCore.Components.Forms;
using BookHeaven.Domain.Features.Authors;
using BookHeaven.Domain.Features.Books;
using BookHeaven.Domain.Features.BookSeries;
using BookHeaven.Domain.Features.BooksProgress;
using BookHeaven.Domain.Features.Profiles;
using BookHeaven.Domain.Services;
using BookHeaven.EpubManager.Epub.Entities;
using BookHeaven.EpubManager.Epub.Services;
using BookHeaven.Server.Abstractions;
using MediatR;

namespace BookHeaven.Server.Services;

public class EpubService(
	IEpubReader epubReader, 
	ISender sender, 
	ILogger<EpubService> logger,
	GlobalEventsService globalEventsService)
	: IFormatService
{
	public async Task<Guid?> LoadFromFile(IBrowserFile file)
	{
		Guid? id;
		try
		{
			var tempPath = Path.GetTempFileName();
			await using (var fileStream = File.Create(tempPath))
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
			
		var epubBook = await epubReader.ReadMetadataAsync(path);
			
		var getBook = await sender.Send(new GetBook.Query(null, epubBook.Metadata.Title));
			
		if (getBook.IsSuccess)
		{
			logger.LogWarning("Book with title '{Title}' already exists in the database, ignoring", epubBook.Metadata.Title);
			return null;
		}
			
		var getAuthor = await sender.Send(new GetAuthor.Query(new GetAuthor.Filter {Name = epubBook.Metadata.Author}));
		if (getAuthor.IsFailure)
		{
			var createAuthor = await sender.Send(new CreateAuthor.Command(epubBook.Metadata.Author));
			if (createAuthor.IsFailure)
			{
				logger.LogError("Failed to create author '{Author}': {Description}", epubBook.Metadata.Author, createAuthor.Error.Description);
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
					logger.LogError("Failed to create series '{Series}': {Description}", epubBook.Metadata.Series, createSeries.Error.Description);
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
			
		var newBook = new Book
		{
			Title = epubBook.Metadata.Title,
			Description = epubBook.Metadata.Description,
			PublishedDate = DateTime.TryParse(epubBook.Metadata.PublishDate, out var tempDate) ? tempDate : null,
			Publisher = epubBook.Metadata.Publisher,
			Language = epubBook.Metadata.Language,
			AuthorId = authorId,
			SeriesId = seriesId,
			SeriesIndex = epubBook.Metadata.SeriesIndex,
			ISBN10 = isbnIdentifiers.FirstOrDefault(x => x.Value.Length == 10)?.Value.Split(":").Last(),
			ISBN13 = isbnIdentifiers.FirstOrDefault(x => x.Value.Length == 13)?.Value.Split(":").Last(),
			ASIN = epubBook.Metadata.Identifiers.FirstOrDefault(x => x.Scheme == "ASIN")?.Value.Split(":").Last(),
			UUID = epubBook.Metadata.Identifiers.FirstOrDefault(x => x.Scheme == "UUID")?.Value.Split(":").Last()
		};
		
		var tempCoverPath = Path.GetTempFileName();
		await StoreCover(epubBook.Cover, tempCoverPath);
		
		var createBook = await sender.Send(new AddBook.Command(newBook, tempCoverPath, epubBook.FilePath));
		if (createBook.IsFailure)
		{
			return null;
		}
			
		var getProfiles = await sender.Send(new GetAllProfiles.Query());
		if (getProfiles.IsFailure)
		{
			logger.LogError("Failed to fetch profiles: {Description}", getProfiles.Error.Description);
			return null;
		}

		foreach (var profile in getProfiles.Value)
		{
			await sender.Send(new CreateBookProgress.Command(createBook.Value, profile.ProfileId));
		}
		
		await globalEventsService.Publish(new BookAdded(createBook.Value));
			
		return createBook.Value;
	}

	public async Task StoreCover(byte[]? image, string dest)
	{
		if (image == null) return;
		var dir = Path.GetDirectoryName(dest);
		if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
		await File.WriteAllBytesAsync(dest, image);
	}
}