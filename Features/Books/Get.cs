using BookHeaven.Domain;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Shared;
using BookHeaven.Server.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace BookHeaven.Server.Features.Books;

public sealed record GetBookQuery(Guid? BookId, string? Title = null): IQuery<Book>;

internal class GetBookQueryHandler(IDbContextFactory<DatabaseContext> dbContextFactory) : IQueryHandler<GetBookQuery, Book>
{
    public async Task<Result<Book>> Handle(GetBookQuery request, CancellationToken cancellationToken)
    {
        if(request.BookId == null && request.Title == null)
        {
            return new Error("Error", "You must provide either a BookId or a Title");
        }
        
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var book = await context.Books.Include(b => b.Author).Include(b => b.Series).FirstOrDefaultAsync(x => x.BookId == request.BookId || x.Title == request.Title, cancellationToken);
        
        return book != null ? book : new Error("Error", "Book not found");
    }
}