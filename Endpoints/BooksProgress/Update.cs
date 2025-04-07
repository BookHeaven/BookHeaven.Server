using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Features.BooksProgress;
using BookHeaven.Server.Abstractions.Api;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BookHeaven.Server.Endpoints.BooksProgress;

public static class Update
{
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("/progress/update", ApiHandler);
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