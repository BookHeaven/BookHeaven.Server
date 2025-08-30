using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using BookHeaven.Server.Abstractions;
using BookHeaven.Server.Entities;
using BookHeaven.Server.MetadataProviders.Abstractions;
using OpenLibraryNET;

namespace BookHeaven.Server.MetadataProviders;

public class OpenLibraryProvider : IMetadataProvider
{
	private const string BaseUrl = "https://openlibrary.org";
	private const string WorkListEndpoint = BaseUrl + "/search.json?q=title:\"{0}\"&lang=spa&_spellcheck_count=0&limit=3&fields=key,cover_i,title,author_name,description,editions,publisher,isbn,id_amazon&mode=everything";
	private const string BookEndpoint = BaseUrl + "{0}.json";
	private const string CoverEndpoint = "https://covers.openlibrary.org/b/id/{0}-L.jpg";


	public async Task<List<BookMetadata>> GetMetadataByName(string name)
	{
		var client = new OpenLibraryClient();
		var parameters = new KeyValuePair<string, string>[]
		{
			new("lang", CultureInfo.CurrentCulture.ThreeLetterISOLanguageName),
			new("_spellcheck_count", "0"),
			new("limit", "3"),
		};
			
		var results = await client.Search.GetSearchResultsAsync(name, parameters);
		if(results == null) return [];
			
		List<BookMetadata> books = [];
		foreach (var result in results)
		{
			var editions = await client.Work.GetEditionsAsync(result.ID, parameters);
			if(editions == null) continue;

			var edition = editions.First();
		}
			
		return [];


		/*List<BookMetadata> books = [];

		var works = await GetWorks(name);

		foreach (var work in works)
		{

			var edition = work.editions.Docs.FirstOrDefault();
			if (edition == null) continue;

			var book = new BookMetadata
			{
				Title = edition.title,
				Author = work.author_name?.FirstOrDefault(),
				Publisher = edition.publisher?.FirstOrDefault(),
				CoverURL = string.Format(CoverEndpoint, edition.cover_i),
				ISBN10 = edition.isbn?.FirstOrDefault(i => i.Length == 10),
				ISBN13 = edition.isbn?.FirstOrDefault(i => i.Length == 13),
				ASIN = edition.id_amazon?.FirstOrDefault()
			};

			var bookData = await GetBookData(edition.key);

			if (bookData?.TryGetProperty("description", out var description) == true)
			{
				if (description.ValueKind == JsonValueKind.Object && description.TryGetProperty("value", out var value))
				{
					book.Description = value.GetString();
				}
			}
			if(bookData?.TryGetProperty("series", out var series) == true)
			{
				book.Series = series.EnumerateArray().FirstOrDefault().GetString();
			}

			books.Add(book);
		}
		return books;*/
	}
		
	private async Task<List<Doc>> GetWorks(string title)
	{
		using var client = new HttpClient();
		var uri = string.Format(WorkListEndpoint, title);
		var request = new HttpRequestMessage(HttpMethod.Get, uri);
		var response = await client.SendAsync(request);

		if (!response.IsSuccessStatusCode) return [];
		var result = await response.Content.ReadAsStringAsync();

		var openLibraryResponse = JsonSerializer.Deserialize<Root>(result);

		return openLibraryResponse?.Docs ?? [];
	}

	private async Task<JsonElement?> GetBookData(string key)
	{
		using var client = new HttpClient();
			
		var request = new HttpRequestMessage(HttpMethod.Get, string.Format(BookEndpoint, key));
		var response = await client.SendAsync(request);

		if (!response.IsSuccessStatusCode) return null;
		var result = await response.Content.ReadAsStringAsync();
				
		return JsonSerializer.Deserialize<JsonElement>(result);
	}
}
	
	

internal class Root
{
	[JsonPropertyName("docs")]
	public List<Doc> Docs { get; set; } = [];
}
	
internal class Doc
{
	[JsonPropertyName("author_name")]
	public List<string> AuthorName { get; set; } = [];

	[JsonPropertyName("cover_i")]
	public int CoverI { get; set; }

	[JsonPropertyName("isbn")]
	public List<string> Isbn { get; set; } = [];

	[JsonPropertyName("key")]
	public string Key { get; set; } = string.Empty;

	[JsonPropertyName("publisher")]
	public List<string> Publisher { get; set; } = [];

	[JsonPropertyName("title")]
	public string Title { get; set; } = string.Empty;

	[JsonPropertyName("id_amazon")]
	public List<string> IdAmazon { get; set; } = [];

	[JsonPropertyName("editions")]
	public Root Editions { get; set; } = new();
}