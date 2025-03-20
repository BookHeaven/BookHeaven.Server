using BookHeaven.Domain;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Shared;
using BookHeaven.Server.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace BookHeaven.Server.Features.Seriess;

public sealed record GetSeriesQuery(Guid? SeriesId, string? Name = null): IQuery<Series>;

internal class GetSeriesQueryHandler(IDbContextFactory<DatabaseContext> dbContextFactory) : IQueryHandler<GetSeriesQuery, Series>
{
    public async Task<Result<Series>> Handle(GetSeriesQuery request, CancellationToken cancellationToken)
    {
        if(request.SeriesId == null && request.Name == null)
        {
            return new Error("Error", "You must provide either an SeriesId or a Name");
        }
        
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        try
        {
            var series = await context.Series.FirstOrDefaultAsync(x => 
                    (request.SeriesId != null && x.SeriesId == request.SeriesId) || 
                    (request.Name != null && x.Name!.ToUpper() == request.Name.ToUpper()),
                cancellationToken);
            
            return series != null ? series : new Error("Error", "Series not found");
        }
        catch (Exception e)
        {
            return new Error("Error", e.Message);
        }
    }
}