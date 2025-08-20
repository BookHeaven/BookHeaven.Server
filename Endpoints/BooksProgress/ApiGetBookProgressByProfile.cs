using BookHeaven.Domain.Features.BooksProgress;
using BookHeaven.Server.Abstractions.Api;
using MediatR;

namespace BookHeaven.Server.Endpoints.BooksProgress;

public static class ApiGetBookProgressByProfile
{
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/profiles/{profileId:guid}/{bookId:guid}", ApiHandler)
                .WithName("GetBookProgressByProfile")
                .WithTags("Book Progress")
                .WithSummary("Get book progress by profile")
                .WithDescription("Retrieves the reading progress of a specific book for a given profile.")
                .Produces<Domain.Entities.BookProgress>()
                .ProducesProblem(StatusCodes.Status500InternalServerError);
        }
        
        private static async Task<IResult> ApiHandler(
            Guid bookId,
            Guid profileId,
            ISender sender,
            ILogger<Endpoint> logger)
        {
            var getProgress = await sender.Send(new GetBookProgressByProfile.Query(bookId, profileId));
            if (getProgress.IsSuccess)
            {
                return Results.Ok(getProgress.Value);
            }
            logger.LogError(getProgress.Error.Description);
            return Results.Problem(getProgress.Error.Description);
        }
    }
}