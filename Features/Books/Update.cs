using BookHeaven.Domain;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Extensions;
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
        
        var book = await context.Books
            .FirstOrDefaultAsync(b => b.BookId == request.Book.BookId, cancellationToken);
        
        if (book == null)
        {
            return new Error("NOT_FOUND", "Book not found");
        }
        
        book.UpdateFrom(request.Book);

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