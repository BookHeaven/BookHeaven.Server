using BookHeaven.Core.Features.BookAnnotations;
using BookHeaven.Server.Features.Api.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace BookHeaven.Server.Features.Api.Endpoints.BookAnnotations;

public static class ApiSyncBookAnnotations
{
    public sealed record BookAnnotationsSyncRequest(Guid BookId, Guid ProfileId, List<BookAnnotation> Annotations);
    
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("/annotations/sync", ApiHandler)
                .WithName("SyncBookAnnotations")
                .WithTags("Book Annotations")
                .Produces<List<BookAnnotation>>(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .Accepts<BookAnnotationsSyncRequest>("application/json")
                .WithSummary("Synchronizes book annotations for a profile.")
                .WithDescription("Merges the submitted annotations with the server annotations using last-write-wins and returns the merged annotations.");
        }

        private static async Task<IResult> ApiHandler(
            [FromBody] BookAnnotationsSyncRequest syncRequest,
            ISender sender,
            ILogger<Endpoint> logger)
        {
            logger.LogInformation(
                "Received {Count} annotations for book {BookId} and profile {ProfileId}",
                syncRequest.Annotations.Count,
                syncRequest.BookId,
                syncRequest.ProfileId);
            var sync = await sender.Send(new SyncBookAnnotations.Command(syncRequest.BookId, syncRequest.ProfileId, syncRequest.Annotations));
            if (sync.IsFailure)
            {
                logger.LogError(sync.Error.Description);
                return Results.Problem(sync.Error.Description);
            }

            return Results.Ok(sync.Value);
        }
    }
}
