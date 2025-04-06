using BookHeaven.Domain;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Shared;
using BookHeaven.Server.Abstractions.Api;
using BookHeaven.Server.Abstractions.Messaging;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookHeaven.Server.Features.BooksProgress;

public static class UpdateBookProgress
{
    public sealed record Command(BookProgress BookProgress) : ICommand;

    internal class Handler(IDbContextFactory<DatabaseContext> dbContextFactory) : ICommandHandler<Command>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            context.BooksProgress.Update(request.BookProgress);

            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                return Result.Failure(new Error("Error", "An error occurred while updating the book progress"));
            }

            return Result.Success();
        }
    }
    
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
				
            var updateProgress = await sender.Send(new Command(progress));
            if (updateProgress.IsFailure)
            {
                logger.LogError(updateProgress.Error.Description);
                return Results.Problem(updateProgress.Error.Description);
            }
            return Results.Ok();
        }
    }
}