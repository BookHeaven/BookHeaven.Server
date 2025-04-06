using BookHeaven.Domain;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Shared;
using BookHeaven.Server.Abstractions.Api;
using BookHeaven.Server.Abstractions.Messaging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BookHeaven.Server.Features.Books;

public static class GetAllBooks
{
    public sealed record Query(string? Filter = null) : IQuery<List<Book>>;

    internal class Handler(IDbContextFactory<DatabaseContext> dbContextFactory) : IQueryHandler<Query, List<Book>>
    {
        public async Task<Result<List<Book>>> Handle(Query request, CancellationToken cancellationToken)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            var books = context.Books
                .Include(b => b.Author)
                .Include(b => b.Series)
                .Include(b => b.Tags).AsQueryable();

            if (!string.IsNullOrEmpty(request.Filter))
            {
                books = books.Where(b =>
                    b.Title!.ToUpper().Contains(request.Filter) ||
                    b.Author!.Name!.ToUpper().Contains(request.Filter) ||
                    b.Series!.Name!.ToUpper().Contains(request.Filter) ||
                    b.Tags.Any(t => t.Name.ToUpper().Contains(request.Filter)));
            }
            else
            {
                books = books.Include(b => b.Progresses.Where(bp => bp.ProfileId == Program.SelectedProfile!.ProfileId));
            }
                

            return books.Any() ? await books.ToListAsync(cancellationToken: cancellationToken) : new Error("Error", "No books found");
        }
    }
    
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/books", ApiHandler);
        }
        
        private static async Task<IResult> ApiHandler(
            ISender sender,
            ILogger<Endpoint> logger)
        {
            var getBooks = await sender.Send(new Query());
            if (getBooks.IsSuccess)
            {
                return Results.Ok(getBooks.Value);
            }
            logger.LogError(getBooks.Error.Description);
            return Results.Problem(getBooks.Error.Description);
        }
    }
}


/*public sealed record GetAllBooksContainingQuery(string Filter) : IQuery<List<Book>>;

internal class GetAllBooksContainingQueryHandler(IDbContextFactory<DatabaseContext> dbContextFactory) : IQueryHandler<GetAllBooksContainingQuery, List<Book>>
{
    public async Task<Result<List<Book>>> Handle(GetAllBooksContainingQuery request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var books = await context.Books
            .Include(b => b.Author)
            .Include(b => b.Series)
            .Include(b => b.Tags)
            .Where(b => 
                b.Title!.ToUpper().Contains(request.Filter) ||
                b.Author!.Name!.ToUpper().Contains(request.Filter) ||
                b.Series!.Name!.ToUpper().Contains(request.Filter) ||
                b.Tags.Any(t => t.Name.ToUpper().Contains(request.Filter)))
            .ToListAsync(cancellationToken);
        return books.Count != 0 ? books : new Error("Error", $"No books found with filter {request.Filter}");
    }
}*/