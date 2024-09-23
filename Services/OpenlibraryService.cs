using BookHeaven.Server.Entities;
using BookHeaven.Server.Interfaces;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BookHeaven.Server.Services
{
	public class OpenlibraryService : IMetadataProviderService
	{
		private const string BaseUrl = "https://openlibrary.org";
		private const string WorkListEndpoint = BaseUrl + "/search.json?q=title:\"{0}\"&lang=spa&_spellcheck_count=0&limit=3&fields=key,cover_i,title,author_name,description,editions,publisher,isbn,id_amazon&mode=everything";
		private const string BookEndpoint = BaseUrl + "{0}.json";
		private const string CoverEndpoint = "https://covers.openlibrary.org/b/id/{0}-L.jpg";


		public async Task<List<BookMetadata>> GetMetadataByName(string name)
		{
			List<BookMetadata> books = [];

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
			return books;
		}
		
		private async Task<List<Doc>> GetWorks(string title)
		{
			using var client = new HttpClient();
			var request = new HttpRequestMessage(HttpMethod.Get, string.Format(WorkListEndpoint, title));
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
		public List<string> author_name { get; set; }

		[JsonPropertyName("cover_i")]
		public int cover_i { get; set; }

		[JsonPropertyName("isbn")]
		public List<string> isbn { get; set; }

		[JsonPropertyName("key")]
		public string key { get; set; }

		[JsonPropertyName("publisher")]
		public List<string> publisher { get; set; }

		[JsonPropertyName("title")]
		public string title { get; set; }

		[JsonPropertyName("id_amazon")]
		public List<string> id_amazon { get; set; }

		[JsonPropertyName("editions")]
		public Root editions { get; set; }
	}
}
