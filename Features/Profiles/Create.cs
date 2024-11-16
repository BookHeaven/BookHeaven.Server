using BookHeaven.Domain;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Shared;
using BookHeaven.Server.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace BookHeaven.Server.Features.Profiles;

public sealed record CreateProfileCommand(string Name) : ICommand<Profile>;

internal class CreateProfileCommandHandler(IDbContextFactory<DatabaseContext> dbContextFactory) : ICommandHandler<CreateProfileCommand, Profile>
{

    public async Task<Result<Profile>> Handle(CreateProfileCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var profile = new Profile
        {
            Name = request.Name
        };

        try 
        {
            await context.Profiles.AddAsync(profile, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            return Result<Profile>.Failure(new Error("Error", e.Message));
        }
        
        return Result<Profile>.Success(profile);
    }
}