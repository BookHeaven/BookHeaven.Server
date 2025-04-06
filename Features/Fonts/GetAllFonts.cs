using BookHeaven.Domain;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Shared;
using BookHeaven.Server.Abstractions.Api;
using BookHeaven.Server.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace BookHeaven.Server.Features.Fonts;

public static class GetAllFonts
{
    public sealed record Query(string? FamilyName = null) : IQuery<List<Font>>;

    internal class QueryHandler(
        IDbContextFactory<DatabaseContext> dbContextFactory,
        ILogger<QueryHandler> logger) : IQueryHandler<Query, List<Font>>
    {
        public async Task<Result<List<Font>>> Handle(Query request, CancellationToken cancellationToken)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            var fonts = await context.Fonts
                .Where(f => request.FamilyName == null || f.Family == request.FamilyName)
                .ToListAsync(cancellationToken);
            return fonts;
        }
    }
    
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/fonts", Handler);
        }

        private static async Task<IResult> Handler(
            IDbContextFactory<DatabaseContext> dbContextFactory,
            ILogger<Endpoint> logger)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync();
            var fonts = context.Fonts.ToList();
            return Results.Ok(fonts);
        }
    }
}