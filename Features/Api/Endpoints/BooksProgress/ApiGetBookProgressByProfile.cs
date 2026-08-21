using BookHeaven.Core.Features.BooksProgress;
using BookHeaven.Server.Features.Api.Abstractions;

namespace BookHeaven.Server.Features.Api.Endpoints.BooksProgress;

public static class ApiGetBookProgressByProfile
{
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/progress/{profileId:guid}/{bookId:guid}", ApiHandler)
                .WithName("GetBookProgressByProfile")
                .WithTags("Book Progress")
                .WithSummary("Get book progress by profile")
                .WithDescription("Retrieves the reading progress of a specific book for a given profile.")
                .Produces<BookProgress>()
                .ProducesProblem(StatusCodes.Status500InternalServerError);

            app.MapGet("/profiles/{profileId:guid}/{bookId:guid}", ApiHandler)
                .WithName("GetBookProgressByProfileDeprecated")
                .WithTags("Book Progress")
                .WithSummary("Get book progress by profile (deprecated)")
                .WithDescription("Retrieves the reading progress of a specific book for a given profile.")
                .Produces<BookProgress>()
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .IsDeprecated();
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