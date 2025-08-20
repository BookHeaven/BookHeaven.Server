using BookHeaven.Domain.Features.Authors;
using BookHeaven.Server.Abstractions.Api;
using MediatR;

namespace BookHeaven.Server.Endpoints.Authors;

public static class ApiGetAllAuthors
{
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/authors", ApiHandler)
                .WithName("GetAllAuthors")
                .WithTags("Authors")
                .WithSummary("Get all authors")
                .WithDescription("Retrieves a list of all authors in the system.")
                .Produces<List<Domain.Entities.Author>>()
                .ProducesProblem(StatusCodes.Status500InternalServerError);
        }

        private static async Task<IResult> ApiHandler(
            ISender sender,
            ILogger<ApiGetAllAuthors.Endpoint> logger)
        {
            var getAuthors = await sender.Send(new GetAllAuthors.Query());
            if (getAuthors.IsSuccess)
            {
                return Results.Ok(getAuthors.Value);
            }
            logger.LogError(getAuthors.Error.Description);
            return Results.Problem(getAuthors.Error.Description);
        }
    }
}