using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Features.BooksProgress;
using BookHeaven.Server.Abstractions.Api;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BookHeaven.Server.Endpoints.BooksProgress;

public static class ApiUpdateBookProgress
{
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("/progress/update", ApiHandler)
                .WithName("UpdateBookProgress")
                .WithTags("Book Progress")
                .Produces(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .Accepts<BookProgress>("application/json")
                .WithSummary("Updates the reading progress of a book for a user profile.")
                .WithDescription("This endpoint allows updating the last read timestamp of a book's progress. If the provided last read time is older than or equal to the existing one, the update is skipped.");
        }

        private static async Task<IResult> ApiHandler(
            [FromBody] BookProgress progress,
            ISender sender,
            ILogger<Endpoint> logger)
        {
            logger.LogInformation($"Received progress update for book {progress.BookProgressId} (LastRead: {progress.LastRead})");
            var getExistingProgress = await sender.Send(new GetBookProgress.Query(progress.BookProgressId));

            if (getExistingProgress.IsSuccess && progress.LastRead <= getExistingProgress.Value.LastRead)
            {
                logger.LogInformation("Last reading time is equal or older than existing one, skipping update");
                return Results.Ok();
            }
				
            logger.LogInformation("Updating progress");
				
            var updateProgress = await sender.Send(new UpdateBookProgress.Command(progress));
            if (updateProgress.IsFailure)
            {
                logger.LogError(updateProgress.Error.Description);
                return Results.Problem(updateProgress.Error.Description);
            }
            return Results.Ok();
        }
    }
}