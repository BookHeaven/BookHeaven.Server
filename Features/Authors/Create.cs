using BookHeaven.Domain;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Shared;
using BookHeaven.Server.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace BookHeaven.Server.Features.Authors;

public sealed record CreateAuthorCommand(
    string Name
) : ICommand<Author>;

internal class CreateAuthorCommandHandler(IDbContextFactory<DatabaseContext> dbContextFactory) : ICommandHandler<CreateAuthorCommand, Author>
{
    public async Task<Result<Author>> Handle(CreateAuthorCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Author author = new()
        {
            Name = request.Name
        };
        
        await context.Authors.AddAsync(author, cancellationToken);
        
        try 
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            return new Error("Error", e.Message);
        }

        return author;
    }
}