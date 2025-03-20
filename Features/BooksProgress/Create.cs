using BookHeaven.Domain;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Shared;
using BookHeaven.Server.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace BookHeaven.Server.Features.BooksProgress;

public sealed record CreateBookProgressCommand(Guid BookId, Guid ProfileId) : ICommand<Guid>;

internal class CreateBookProgressCommandHandler(IDbContextFactory<DatabaseContext> dbContextFactory) : ICommandHandler<CreateBookProgressCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateBookProgressCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var progress = new BookProgress
        {
            BookId = request.BookId,
            ProfileId = request.ProfileId
        };

        try
        {
            await context.BooksProgress.AddAsync(progress, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            return new Error("Error", e.Message);
        }
        return progress.BookProgressId;
    }
}