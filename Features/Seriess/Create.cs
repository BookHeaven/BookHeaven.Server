using BookHeaven.Domain;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Shared;
using BookHeaven.Server.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace BookHeaven.Server.Features.Seriess;

public sealed record CreateSeriesCommand(
    string Name
) : ICommand<Series>;

internal class CreateSeriesCommandHandler(IDbContextFactory<DatabaseContext> dbContextFactory) : ICommandHandler<CreateSeriesCommand, Series>
{
    public async Task<Result<Series>> Handle(CreateSeriesCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Series series = new()
        {
            Name = request.Name
        };
        
        await context.Series.AddAsync(series, cancellationToken);
        
        try 
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception e)
        {
            return Result<Series>.Failure(new("Error", e.Message));
        }

        return Result<Series>.Success(series);
    }
}