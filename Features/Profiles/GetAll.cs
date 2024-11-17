using BookHeaven.Domain;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Shared;
using BookHeaven.Server.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace BookHeaven.Server.Features.Profiles;

public sealed record GetAllProfilesQuery : IQuery<List<Profile>>;

internal class GetAllProfilesQueryHandler(IDbContextFactory<DatabaseContext> dbContextFactory)
    : IQueryHandler<GetAllProfilesQuery, List<Profile>>
{
    public async Task<Result<List<Profile>>> Handle(GetAllProfilesQuery request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var profiles = await context.Profiles.ToListAsync(cancellationToken);

        return Result<List<Profile>>.Success(profiles);
    }
}