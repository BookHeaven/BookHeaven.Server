using BookHeaven.Domain.Features.Books;
using BookHeaven.Server.Abstractions.Api;
using MediatR;

namespace BookHeaven.Server.Endpoints.Books;

public static class ApiGetAllBooks
{
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/books", ApiHandler)
                .WithName("GetAllBooks")
                .WithTags("Books")
                .WithSummary("Get all books")
                .WithDescription("Retrieves a list of all books in the system.")
                .Produces<List<Domain.Entities.Book>>()
                .ProducesProblem(StatusCodes.Status500InternalServerError);
        }
        
        private static async Task<IResult> ApiHandler(
            ISender sender,
            ILogger<Endpoint> logger)
        {
            var getBooks = await sender.Send(new GetAllBooks.Query());
            if (getBooks.IsSuccess)
            {
                return Results.Ok(getBooks.Value);
            }
            logger.LogError(getBooks.Error.Description);
            return Results.Problem(getBooks.Error.Description);
        }
    }
}