using BookHeaven.Domain;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Shared;
using BookHeaven.Server.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace BookHeaven.Server.Features.Books;
public sealed record GetAllBooksQuery : IQuery<List<Book>>;

internal class GetAllBooksQueryHandler(IDbContextFactory<DatabaseContext> dbContextFactory) : IQueryHandler<GetAllBooksQuery, List<Book>>
{
    public async Task<Result<List<Book>>> Handle(GetAllBooksQuery request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var books = await context.Books
            .Include(b => b.Author)
            .Include(b => b.Series)
            .Include(b => b.Progresses.Where(bp => bp.ProfileId == Program.SelectedProfile!.ProfileId))
            .ToListAsync(cancellationToken);

        return books.Any() ? Result<List<Book>>.Success(books) : Result<List<Book>>.Failure(new Error("Error", "No books found"));
    }
}

public sealed record GetAllBooksContainingQuery(string Filter) : IQuery<List<Book>>;

internal class GetAllBooksContainingQueryHandler(IDbContextFactory<DatabaseContext> dbContextFactory) : IQueryHandler<GetAllBooksContainingQuery, List<Book>>
{
    public async Task<Result<List<Book>>> Handle(GetAllBooksContainingQuery request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var books = await context.Books
            .Include(b => b.Author)
            .Include(b => b.Series)
            .Where(b => 
                b.Title!.ToUpper().Contains(request.Filter) ||
                b.Author!.Name!.ToUpper().Contains(request.Filter) ||
                b.Series!.Name!.ToUpper().Contains(request.Filter))
            .ToListAsync(cancellationToken);
        return books.Any() ? Result<List<Book>>.Success(books) : Result<List<Book>>.Failure(new Error("Error", $"No books found with filter {request.Filter}"));
    }
}