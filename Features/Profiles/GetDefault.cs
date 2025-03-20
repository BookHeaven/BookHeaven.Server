using BookHeaven.Domain;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Shared;
using BookHeaven.Server.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace BookHeaven.Server.Features.Profiles;

public class GetDefaultProfileQuery() : IQuery<Profile>;

internal class GetDefaultProfileQueryHandler(IDbContextFactory<DatabaseContext> dbContextFactory) : IQueryHandler<GetDefaultProfileQuery, Profile>
{
    public async Task<Result<Profile>> Handle(GetDefaultProfileQuery request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var profile = await context.Profiles.FirstOrDefaultAsync(p => p.Name == "Default", cancellationToken);

        return profile != null ? profile : new Error("Error", "Default profile not found");
    }
}