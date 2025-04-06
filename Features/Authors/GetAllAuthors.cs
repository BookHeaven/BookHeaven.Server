using BookHeaven.Domain;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Shared;
using BookHeaven.Server.Abstractions.Api;
using BookHeaven.Server.Abstractions.Messaging;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookHeaven.Server.Features.Authors;

public static class GetAllAuthors
{
    public sealed record Query(bool IncludeBooks = false) : IQuery<List<Author>>;

    internal class Handler(IDbContextFactory<DatabaseContext> dbContextFactory)
        : IQueryHandler<Query, List<Author>>
    {
        public async Task<Result<List<Author>>> Handle(Query request, CancellationToken cancellationToken)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            return request.IncludeBooks
                ? await context.Authors.Include(x => x.Books).ThenInclude(b => b.Series).ToListAsync(cancellationToken)
                : await context.Authors.ToListAsync(cancellationToken);
        }
    }

    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/authors", ApiHandler);
        }

        private static async Task<IResult> ApiHandler(
            ISender sender,
            ILogger<Endpoint> logger)
        {
            var getAuthors = await sender.Send(new Query());
            if (getAuthors.IsSuccess)
            {
                return Results.Ok(getAuthors.Value);
            }
            logger.LogError(getAuthors.Error.Description);
            return Results.Problem(getAuthors.Error.Description);
        }
    }
}
