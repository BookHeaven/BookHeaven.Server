using BookHeaven.Server.Entities;
using BookHeaven.Server.MetadataProviders.Abstractions;
using Google.Apis.Books.v1;
using Google.Apis.Services;

namespace BookHeaven.Server.MetadataProviders;

public class GoogleBooksProvider : IMetadataProvider
{
    private readonly List<string> _dateFormats = ["yyyy-MM-dd", "yyyy-MM", "yyyy"];
    
    public async Task<List<BookMetadata>> GetMetadataByName(string name)
    {
        var service = new BooksService(new BaseClientService.Initializer
        {
            ApiKey = Environment.GetEnvironmentVariable("GOOGLE_BOOKS_API_KEY"),
            ApplicationName = "BookHeaven"
        });
        
        var request = service.Volumes.List(name);
        var response = await request.ExecuteAsync();
        
        return response.Items.Select(item => new BookMetadata
        {
            Title = item.VolumeInfo.Title,
            Author = item.VolumeInfo.Authors?.FirstOrDefault(),
            Publisher = item.VolumeInfo.Publisher,
            PublishedDate = DateTime.TryParseExact(item.VolumeInfo.PublishedDate, _dateFormats.ToArray(), null, System.Globalization.DateTimeStyles.None, out var date) ? date : null,
            CoverUrl = AppendFifeParam(item.VolumeInfo.ImageLinks?.Large ?? item.VolumeInfo.ImageLinks?.Medium ?? item.VolumeInfo.ImageLinks?.Small ?? item.VolumeInfo.ImageLinks?.Thumbnail ?? item.VolumeInfo.ImageLinks?.SmallThumbnail ?? string.Empty),
            Isbn10 = item.VolumeInfo.IndustryIdentifiers?.FirstOrDefault(i => i.Type == "ISBN_10")?.Identifier,
            Isbn13 = item.VolumeInfo.IndustryIdentifiers?.FirstOrDefault(i => i.Type == "ISBN_13")?.Identifier,
            Description = item.VolumeInfo.Description
        }).ToList();
    }
    
    private static string AppendFifeParam(string url)
    {
        return string.IsNullOrEmpty(url) ? string.Empty : url + "&fife=w-800";
    }
}