using BookHeaven.Domain;
using BookHeaven.Domain.Extensions;
using BookHeaven.Domain.Shared;
using BookHeaven.Server.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore;

namespace BookHeaven.Server.Features.Fonts;

public static class DeleteFont
{
    public sealed record Command(string FamilyName) : ICommand;
    
    internal class Handler(
        IDbContextFactory<DatabaseContext> dbContextFactory,
        ILogger<Handler> logger) : ICommandHandler<Command>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);

            var variants = await context.Fonts.Where(v => v.Family == request.FamilyName).ToListAsync(cancellationToken: cancellationToken);

            try
            {
                var folder = Path.Combine(Program.FontsPath, request.FamilyName);
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, true);
                }
                
                context.Fonts.RemoveRange(variants);

                await context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return new Error("Error deleting font");
            }

            return Result.Success();
        }
    }
}