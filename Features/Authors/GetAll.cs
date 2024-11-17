using BookHeaven.Domain;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Shared;
using BookHeaven.Server.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace BookHeaven.Server.Features.Authors;

public sealed record GetAllAuthorsQuery(bool IncludeBooks = false) : IQuery<List<Author>>;

internal class GetAllAuthorsQueryHandler(IDbContextFactory<DatabaseContext> dbContextFactory)
    : IQueryHandler<GetAllAuthorsQuery, List<Author>>
{
    public async Task<Result<List<Author>>> Handle(GetAllAuthorsQuery request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var authors = request.IncludeBooks
            ? await context.Authors.Include(x => x.Books).ThenInclude(b => b.Series).ToListAsync(cancellationToken)
            : await context.Authors.ToListAsync(cancellationToken);
        
        return Result<List<Author>>.Success(authors);
    }
}