using System.Globalization;
using BookHeaven.Server.Entities;
using BookHeaven.Server.MetadataProviders.Abstractions;
using OpenLibraryNET;
using OpenLibraryNET.Utility;

namespace BookHeaven.Server.MetadataProviders;

public class OpenLibraryProvider : IMetadataProvider
{
	public async Task<List<BookMetadata>> GetMetadataByName(string name)
	{
		var client = new OpenLibraryClient();
		var parameters = new KeyValuePair<string, string>[]
		{
			new("lang", CultureInfo.CurrentCulture.TwoLetterISOLanguageName),
			new("_spellcheck_count", "0"),
			new("limit", "3"),
			new("sort", "editions"),
		};
			
		var results = await client.Search.GetSearchResultsAsync(name, parameters);
		if(results == null) return [];
			
		List<BookMetadata> books = [];
		var work = results.First();
		
		var editions = await client.Work.GetEditionsAsync(work.ID, parameters);
		if(editions == null) return [];
		foreach (var edition in editions)
		{
			var cover = await client.Image.GetCoverAsync(CoverIdType.OLID, edition.ID, ImageSize.Large);
			
			var book = new BookMetadata
			{
				Title = edition.Title,
				Author = work.ExtensionData?.FirstOrDefault(k => k.Key == "author_name").Value?.FirstOrDefault()?.ToString(),
				Publisher = edition.Publishers[0],
				CoverUrl = "data:image/jpeg;base64," + Convert.ToBase64String(cover),
			};
			
			books.Add(book);
		}
			
		return books;
	}
}