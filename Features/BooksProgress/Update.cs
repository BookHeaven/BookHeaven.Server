using BookHeaven.Domain;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Shared;
using BookHeaven.Server.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace BookHeaven.Server.Features.BooksProgress;

public sealed record UpdateBookProgressCommand(BookProgress BookProgress) : ICommand;

internal class UpdateBookProgressCommandHandler(IDbContextFactory<DatabaseContext> dbContextFactory) : ICommandHandler<UpdateBookProgressCommand>
{
    public async Task<Result> Handle(UpdateBookProgressCommand request, CancellationToken cancellationToken)
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