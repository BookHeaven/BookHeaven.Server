using BookHeaven.Domain;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Shared;
using BookHeaven.Server.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace BookHeaven.Server.Features.Authors;

public sealed record GetAuthorQuery(Guid? AuthorId, string? Name): IQuery<Author>;

internal class GetAuthorQueryHandler(IDbContextFactory<DatabaseContext> dbContextFactory) : IQueryHandler<GetAuthorQuery, Author>
{
    public async Task<Result<Author>> Handle(GetAuthorQuery request, CancellationToken cancellationToken)
    {
        if(request.AuthorId == null && request.Name == null)
        {
            return Result<Author>.Failure(new Error("Error", "You must provide either an AuthorId or a Name"));
        }
        
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        try
        {
            var author = await context.Authors.FirstOrDefaultAsync(x => 
                    (request.AuthorId != null && x.AuthorId == request.AuthorId) || 
                    (request.Name != null && x.Name!.ToUpper() == request.Name.ToUpper())
                , cancellationToken);
            
            return author == null ? Result<Author>.Failure(new Error("Error", "Author not found")) : Result<Author>.Success(author);
        }
        catch (Exception e)
        {
            return Result<Author>.Failure(new Error("Error", e.Message));
        }
    }
}