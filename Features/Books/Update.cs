using BookHeaven.Domain;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Shared;
using BookHeaven.Server.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace BookHeaven.Server.Features.Books;

public sealed record UpdateBookCommand(Book Book) : ICommand;

internal class UpdateBookCommandHandler(IDbContextFactory<DatabaseContext> dbContextFactory) : ICommandHandler<UpdateBookCommand>
{
    public async Task<Result> Handle(UpdateBookCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        context.Books.Update(request.Book);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result.Failure(new Error("Error", "An error occurred while updating the book"));
        }

        return Result.Success();
    }
}