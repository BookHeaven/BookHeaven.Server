using BookHeaven.Core.Features.ReadingHistory;
using BookHeaven.Server.Features.Api.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace BookHeaven.Server.Features.Api.Endpoints.ReadingHistory;

public static class ApiSyncReadingHistory
{
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("/history/sync", ApiHandler)
                .WithName("SyncReadingHistory")
                .WithTags("Reading History")
                .Produces(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .Accepts<List<ReadingHistoryEntry>>("application/json")
                .WithSummary("Merges reading history entries sent by a reader.")
                .WithDescription("Adds the entries that don't exist yet on the server. Entries are identified by profile, book, start date and finish date. Existing entries are never modified or removed.");
        }

        private static async Task<IResult> ApiHandler(
            [FromBody] List<ReadingHistoryEntry> entries,
            ISender sender,
            ILogger<Endpoint> logger)
        {
            logger.LogInformation("Received {Count} reading history entries", entries.Count);
            var sync = await sender.Send(new SyncReadingHistory.Command(entries));
            if (sync.IsFailure)
            {
                logger.LogError(sync.Error.Description);
                return Results.Problem(sync.Error.Description);
            }
            return Results.Ok();
        }
    }
}
