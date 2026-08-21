using BookHeaven.Core.Features.Books;
using BookHeaven.Core.Shared;
using BookHeaven.Server.Features.Api.Abstractions;

namespace BookHeaven.Server.Features.Api.Endpoints.Books;

public static class ApiGetBooks
{
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/books", ApiHandler)
                .WithName("GetBooks")
                .WithTags("Books")
                .WithSummary("Get list of books")
                .WithDescription("Retrieves a list of all books, optionally filtered by collection ID or a search filter. When filtering by collection, the profile ID is required to check reading status.")
                .Produces<List<Book>>()
                .ProducesProblem(StatusCodes.Status500InternalServerError);
        }
        
        private static async Task<IResult> ApiHandler(
            ISender sender,
            ILogger<Endpoint> logger,
            Guid? profileId = null,
            Guid? collectionId = null,
            string? filter = null)
        {
            Result<List<Book>> getBooks;

            if (collectionId.HasValue)
            {
                if (profileId is null)
                {
                    logger.LogError("Profile ID is required when filtering by collection.");
                    return Results.Problem("Profile ID is required when filtering by collection.");
                }
                getBooks = await sender.Send(new GetBooksByCollection.Query(collectionId.Value, profileId.Value));
            }
            else
            {
                getBooks = await sender.Send(new GetAllBooks.Query(profileId, filter));
            }
            
            if (getBooks.IsFailure)
            {
                logger.LogError(getBooks.Error.Description);
                return Results.Problem(getBooks.Error.Description);
                
            }
            return Results.Ok(getBooks.Value);
        }
    }
}