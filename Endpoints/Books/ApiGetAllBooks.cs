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
            app.MapGet("/books", ApiHandler);
        }
        
        private static async Task<IResult> ApiHandler(
            ISender sender,
            ILogger<Endpoint> logger)
        {
            var getBooks = await sender.Send(new GetAllBooks.Query(Program.SelectedProfile!.ProfileId));
            if (getBooks.IsSuccess)
            {
                return Results.Ok(getBooks.Value);
            }
            logger.LogError(getBooks.Error.Description);
            return Results.Problem(getBooks.Error.Description);
        }
    }
}