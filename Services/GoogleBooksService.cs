using BookHeaven.Server.Abstractions;
using BookHeaven.Server.Entities;
using Google.Apis.Books.v1;
using Google.Apis.Services;

namespace BookHeaven.Server.Services;

public class GoogleBooksService : IMetadataProviderService
{
    public async Task<List<BookMetadata>> GetMetadataByName(string name)
    {
        var service = new BooksService(new BaseClientService.Initializer
        {
            ApiKey = Environment.GetEnvironmentVariable("GOOGLE_BOOKS_API_KEY"),
            ApplicationName = "BookHeaven"
        });
        
        var request = service.Volumes.List(name);
        var response = await request.ExecuteAsync();
        
        return response.Items.Take(1).Select(item => new BookMetadata
        {
            Title = item.VolumeInfo.Title,
            Author = item.VolumeInfo.Authors?.FirstOrDefault(),
            Publisher = item.VolumeInfo.Publisher,
            PublishedDate = DateTime.Parse(item.VolumeInfo.PublishedDate),
            CoverUrl = item.VolumeInfo.ImageLinks?.Large ?? item.VolumeInfo.ImageLinks?.Medium ?? item.VolumeInfo.ImageLinks?.Small ?? item.VolumeInfo.ImageLinks?.Thumbnail ?? item.VolumeInfo.ImageLinks?.SmallThumbnail ?? string.Empty,
            Isbn10 = item.VolumeInfo.IndustryIdentifiers?.FirstOrDefault(i => i.Type == "ISBN_10")?.Identifier,
            Isbn13 = item.VolumeInfo.IndustryIdentifiers?.FirstOrDefault(i => i.Type == "ISBN_13")?.Identifier,
            Description = item.VolumeInfo.Description
        }).ToList();
    }
}