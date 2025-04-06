using BookHeaven.Domain;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Shared;
using BookHeaven.Server.Abstractions.Api;
using BookHeaven.Server.Abstractions.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BookHeaven.Server.Features.Profiles;

public static class GetAllProfiles
{
    public sealed record Query : IQuery<List<Profile>>;

    internal class Handler(IDbContextFactory<DatabaseContext> dbContextFactory)
        : IQueryHandler<Query, List<Profile>>
    {
        public async Task<Result<List<Profile>>> Handle(Query request, CancellationToken cancellationToken)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            return await context.Profiles.ToListAsync(cancellationToken);
        }
    }
    
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/profiles", ApiHandler);
        }
        
        private static async Task<IResult> ApiHandler(
            ISender sender,
            ILogger<Endpoint> logger)
        {
            var getProfiles = await sender.Send(new Query());
            if (getProfiles.IsSuccess)
            {
                return Results.Ok(getProfiles.Value);
            }
            logger.LogError(getProfiles.Error.Description);
            return Results.Problem(getProfiles.Error.Description);
        }
        
    }
}
