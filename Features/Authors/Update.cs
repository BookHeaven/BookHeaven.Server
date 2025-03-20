using BookHeaven.Domain;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Shared;
using BookHeaven.Server.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace BookHeaven.Server.Features.Authors;

public sealed record UpdateAuthorCommand(Author Author) : ICommand<Author>;

internal class UpdateAuthorCommandHandler(IDbContextFactory<DatabaseContext> dbContextFactory) : ICommandHandler<UpdateAuthorCommand, Author>
{
    public async Task<Result<Author>> Handle(UpdateAuthorCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        
        context.Authors.Update(request.Author);
        
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return new Error("Error", "An error occurred while updating the author");
        }
        
        return request.Author;
    }
}