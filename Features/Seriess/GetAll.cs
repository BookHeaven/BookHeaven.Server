using BookHeaven.Domain;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Shared;
using BookHeaven.Server.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace BookHeaven.Server.Features.Seriess;

public sealed record GetAllSeriesQuery(bool IncludeBooks = false) : IQuery<List<Series>>;

internal class GetAllSeriesQueryHandler(IDbContextFactory<DatabaseContext> dbContextFactory)
    : IQueryHandler<GetAllSeriesQuery, List<Series>>
{
    public async Task<Result<List<Series>>> Handle(GetAllSeriesQuery request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        var series = request.IncludeBooks
            ? await context.Series.Include(x => x.Books).ToListAsync(cancellationToken)
            : await context.Series.ToListAsync(cancellationToken);

        return Result<List<Series>>.Success(series);
    }
}