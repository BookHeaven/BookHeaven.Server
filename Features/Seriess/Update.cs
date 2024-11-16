using BookHeaven.Domain;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Shared;
using BookHeaven.Server.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace BookHeaven.Server.Features.Seriess;

public sealed record UpdateSeriesCommand(Series Series) : ICommand<Series>;

internal class UpdateSeriesCommandHandler(IDbContextFactory<DatabaseContext> dbContextFactory)
    : ICommandHandler<UpdateSeriesCommand, Series>
{
    public async Task<Result<Series>> Handle(UpdateSeriesCommand request, CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        context.Series.Update(request.Series);

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Result<Series>.Failure(new Error("Error", "An error occurred while updating the series"));
        }

        return Result<Series>.Success(request.Series);
    }
}