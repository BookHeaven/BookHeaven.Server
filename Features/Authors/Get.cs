using BookHeaven.Domain;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Shared;
using BookHeaven.Server.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace BookHeaven.Server.Features.Authors;

public sealed record AuthorRequest
{
    public Guid? AuthorId { get; init; }
    public string? Name { get; init; }
    public bool IncludeBooks { get; init; }
}

public sealed record GetAuthorQuery(AuthorRequest Request): IQuery<Author>;

internal class GetAuthorQueryHandler(IDbContextFactory<DatabaseContext> dbContextFactory) : IQueryHandler<GetAuthorQuery, Author>
{
    public async Task<Result<Author>> Handle(GetAuthorQuery query, CancellationToken cancellationToken)
    {
        if(query.Request.AuthorId == null && query.Request.Name == null)
        {
            return Result<Author>.Failure(new Error("Error", "You must provide either an AuthorId or a Name"));
        }
        
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        try
        {
            var dbQuery = context.Authors.Where(x => 
                    (query.Request.AuthorId != null && x.AuthorId == query.Request.AuthorId) || 
                    (query.Request.Name != null && x.Name!.ToUpper() == query.Request.Name.ToUpper()));
            
            if (query.Request.IncludeBooks)
            {
                dbQuery = dbQuery
                    .Include(a => a.Books)
                    .ThenInclude(b => b.Series)
                    .Include(a => a.Books)
                    .ThenInclude(b => b.Progresses.Where(bp => bp.ProfileId == Program.SelectedProfile!.ProfileId));
            }

            var author = await dbQuery.FirstOrDefaultAsync(cancellationToken: cancellationToken);
            
            return author == null ? Result<Author>.Failure(new Error("Error", "Author not found")) : Result<Author>.Success(author);
        }
        catch (Exception e)
        {
            return Result<Author>.Failure(new Error("Error", e.Message));
        }
    }
}