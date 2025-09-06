using BookHeaven.Domain.Shared;
using BookHeaven.Server.Entities;
using BookHeaven.Server.MetadataProviders.Abstractions;
using BookHeaven.Server.MetadataProviders.DTO;
using Google.Apis.Books.v1;
using Google.Apis.Services;

namespace BookHeaven.Server.MetadataProviders;

public class GoogleBooksProvider(ILogger<GoogleBooksProvider> logger) : IMetadataProvider
{
    private readonly List<string> _dateFormats = ["yyyy-MM-dd", "yyyy-MM", "yyyy"];
    
    public async Task<Result<List<BookMetadata>>> GetMetadataAsync(MetadataRequest request)
    {
        var filter = request.Title + (string.IsNullOrWhiteSpace(request.Author) ? string.Empty : $" {request.Author}");
        try
        {
            var service = new BooksService(
                new BaseClientService.Initializer
                {
                    ApiKey = Environment.GetEnvironmentVariable("GOOGLE_BOOKS_API_KEY"),
                    ApplicationName = "BookHeaven"
                });

            var getVolumes = service.Volumes.List(filter);
            var response = await getVolumes.ExecuteAsync();

            return response.Items.Select(item => new BookMetadata
                {
                    Title = item.VolumeInfo.Title,
                    Author = item.VolumeInfo.Authors?.FirstOrDefault(),
                    Publisher = item.VolumeInfo.Publisher,
                    PublishedDate = DateTime.TryParseExact(item.VolumeInfo.PublishedDate, _dateFormats.ToArray(), null, System.Globalization.DateTimeStyles.None, out var date) ? date : null,
                    Isbn10 = item.VolumeInfo.IndustryIdentifiers?.FirstOrDefault(i => i.Type == "ISBN_10")?.Identifier,
                    Isbn13 = item.VolumeInfo.IndustryIdentifiers?.FirstOrDefault(i => i.Type == "ISBN_13")?.Identifier,
                    Description = item.VolumeInfo.Description
                })
                .ToList();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error fetching metadata from Google Books for book: {BookName}", request.Title);
            return new Error("Unexpected error while fetching metadata");
        }
    }
}