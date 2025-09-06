using System.Collections.ObjectModel;
using System.Globalization;
using BookHeaven.Domain.Shared;
using BookHeaven.Server.Entities;
using BookHeaven.Server.MetadataProviders.Abstractions;
using BookHeaven.Server.MetadataProviders.DTO;
using Newtonsoft.Json.Linq;
using OpenLibraryNET;
using OpenLibraryNET.Utility;

namespace BookHeaven.Server.MetadataProviders;

public class OpenLibraryProvider(ILogger<OpenLibraryProvider> logger) : IMetadataProvider
{
	private readonly List<string> _dateFormats = ["yyyy-MM-dd", "yyyy-MM", "yyyy"];
	
	public async Task<Result<List<BookMetadata>>> GetMetadataAsync(MetadataRequest request)
	{
		List<BookMetadata> books = [];
		try
		{
			var client = new OpenLibraryClient();
			var parameters = new KeyValuePair<string, string>[]
			{
				new("lang", CultureInfo.CurrentCulture.TwoLetterISOLanguageName),
				new("_spellcheck_count", "0"),
				new("limit", "3"),
			};
			
			var filter = request.Title + (string.IsNullOrWhiteSpace(request.Author) ? string.Empty : $" {request.Author}");
			
			var works = await client.Search.GetSearchResultsAsync(filter, parameters);
			if(works == null) return books;
			
		
			foreach (var work in works)
			{
				var editions = await client.Work.GetEditionsAsync(work.ID, parameters);
				if(editions is null or {Length: 0}) continue;

				foreach (var edition in editions)
				{
					var metadata = new BookMetadata
					{
						Title = work.Title,
						Author = GetExtensionDataValue(work.ExtensionData, "author_name"),
						Description = work.Description,
						Publisher = edition.Publishers[0],
						PublishedDate = DateTime.TryParseExact(GetExtensionDataValue(edition.ExtensionData, "publish_date"), _dateFormats.ToArray(), null, DateTimeStyles.None, out var parsedDate) ? parsedDate : null,
						Isbn10 = edition.ISBN10.FirstOrDefault(),
						Isbn13 = edition.ISBN13.FirstOrDefault(),
					};
				
					books.Add(metadata);
				}
			}
			
			return books;
		}
		catch (Exception e)
		{
			logger.LogError(e, "Error fetching metadata from OpenLibrary for book: {BookName}", request.Title);
			return new Error("Unexpected error while fetching metadata");
		}
	}
	
	private static string GetExtensionDataValue(ReadOnlyDictionary<string, JToken>? extensionData, string key)
	{
		if(extensionData is null or {Count: 0} || !extensionData.TryGetValue(key, out var value)) return string.Empty;

		if(value.Type == JTokenType.Array) 
			return value.First?.Value<string>() ?? string.Empty;
		
		return value.Value<string>() ?? string.Empty;
	}
}