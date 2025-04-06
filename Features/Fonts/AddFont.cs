using BookHeaven.Domain;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Extensions;
using BookHeaven.Domain.Shared;
using BookHeaven.Server.Abstractions.Messaging;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;

namespace BookHeaven.Server.Features.Fonts;

public static class AddFont
{
    public sealed record Command(Font Font, IBrowserFile File) : ICommand<Font>;
    
    internal class CommandHandler(
        IDbContextFactory<DatabaseContext> dbContextFactory,
        ILogger<CommandHandler> logger) : ICommandHandler<Command, Font>
    {
        public async Task<Result<Font>> Handle(Command request, CancellationToken cancellationToken)
        {
            await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            
            var font = new Font
            {
                Family = request.Font.Family,
                Style = request.Font.Style,
                Weight = request.Font.Weight,
                FileName = request.Font.FileName
            };

            context.Fonts.Add(font);

            try
            {
                await context.SaveChangesAsync(cancellationToken);
                var filePath = font.File(Program.FontsPath);

                if (!Directory.Exists(font.Folder(Program.FontsPath)))
                {
                    Directory.CreateDirectory(font.Folder(Program.FontsPath));
                }

                await using var fileStream = File.Create(filePath);
                await request.File.OpenReadStream(maxAllowedSize: 1048576 * 3,cancellationToken: cancellationToken).CopyToAsync(fileStream, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error adding font");
                return new Error("FONT_ADD_ERROR", ex.Message);
            }

            return font;
        }
    }
}