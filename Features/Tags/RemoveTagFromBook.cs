using BookHeaven.Domain;
using BookHeaven.Domain.Shared;
using BookHeaven.Server.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace BookHeaven.Server.Features.Tags;

public static class RemoveTagFromBook
{
    public sealed record Command(Guid TagId, Guid BookId) : ICommand;
    
    internal class CommandHandler(
        IDbContextFactory<DatabaseContext> dbContextFactory,
        ILogger<CommandHandler> logger) : ICommandHandler<Command>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            
            var book = await context.Books
                .Include(b => b.Tags)
                .FirstOrDefaultAsync(b => b.BookId == request.BookId, cancellationToken);

            if (book == null)
            {
                return new Error("BOOK_NOT_FOUND", "Book not found");
            }

            var tag = book.Tags.FirstOrDefault(t => t.TagId == request.TagId);

            if (tag == null)
            {
                return new Error("TAG_NOT_FOUND", "Tag not found");
            }

            book.Tags.Remove(tag);

            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error removing tag from book");
                return new Error("TAG_REMOVE_ERROR", ex.Message);
            }

            return Result.Success();
        }
    }
}