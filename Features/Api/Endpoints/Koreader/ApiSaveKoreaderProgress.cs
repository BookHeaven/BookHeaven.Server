using System.Text.RegularExpressions;
using BookHeaven.Core.Features.Books;
using BookHeaven.Core.Features.BooksProgress;
using BookHeaven.Core.Features.KoreaderProgress;
using BookHeaven.Core.Features.Profiles;
using BookHeaven.Server.Features.Api.Abstractions;

namespace BookHeaven.Server.Features.Api.Endpoints.Koreader;

public static class ApiSaveKoreaderProgress
{
    private static readonly Regex DocFragmentRegex = new("DocFragment\\[(\\d+)\\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private class KOReaderDocumentRequest
    {
        public string document { get; set; } = null!;
        public string progress { get; set; } = null!;
        public decimal percentage { get; set; }
        public string device { get; set; } = null!;
        public string device_id { get; set; } = null!;
    }
    
    public class Endpoint : IKoreaderSyncEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("/syncs/progress", Handler)
                .ExcludeFromDescription();
        }

        private static async Task<IResult> Handler(
            KOReaderDocumentRequest request,
            ISender sender,
            IHttpContextAccessor httpContextAccessor,
            ILogger<Endpoint> logger)
        {
            logger.LogInformation("Received KOReader progress sync request for document {Document}", request.document);
            var profileName = httpContextAccessor.HttpContext?.Request.Headers["x-auth-user"].ToString();
             
            var getProfile = await sender.Send(new GetProfileByName.Query(profileName ?? ""));
            if (getProfile.IsFailure)
            {
                logger.LogWarning("Profile with name {ProfileName} not found.", profileName);
                return Results.Unauthorized();
            }

            var koProgress = new KoreaderProgress
            {
                ProfileId = getProfile.Value.ProfileId,
                DeviceId = request.device_id,
                DeviceName = request.device,
                DocumentHash = request.document,
                Progress = request.progress,
                Percentage = request.percentage,
                Timestamp = DateTime.UtcNow
            };

            var chapterNumber = GetKoreaderChapterNumber(request.progress);
            if (chapterNumber is null)
            {
                logger.LogError("Unable to parse KOReader chapter from progress '{Progress}' for document '{Document}'.", request.progress, request.document);
            }
                
            logger.LogInformation("Saving KOReader progress with hash '{Document}' for profile '{ProfileName}'.", request.document, profileName);
            await sender.Send(new SaveKoreaderProgress.Command(koProgress));
             
            var getBook = await sender.Send(new GetBook.Query { Hash = request.document });
            if (getBook.IsFailure)
            {
                logger.LogWarning("Book with hash '{Hash}' not found in database, can't update bookheaven progress.", request.document);
            }
            else
            {
                logger.LogInformation("Saving BookHeaven progress for book '{BookTitle}'[{Hash}] and profile '{ProfileName}'.", getBook.Value.Title, request.document ,profileName);
                var getProgress = await sender.Send(new GetBookProgressByProfile.Query(getBook.Value.BookId, getProfile.Value.ProfileId));
                var progress = getProgress.Value;

                progress.StartDate ??= DateTimeOffset.Now;
                progress.Progress = request.percentage * 100;
                progress.LastRead = DateTimeOffset.Now;
                if (chapterNumber is not null)
                {
                    progress.Chapter = chapterNumber.Value - 1;
                    progress.ChapterProgress = 0;
                }
                if (progress.Progress >= 100)
                {
                    progress.EndDate ??= DateTimeOffset.Now;
                }
                
                await sender.Send(new UpdateBookProgress.Command(progress));
            }
            
            return Results.Ok(new
            {
                document = request.document,
                timestamp = koProgress.Timestamp,
            });
        }
    }
    
    private static int? GetKoreaderChapterNumber(string? progress)
    {
        if (string.IsNullOrWhiteSpace(progress))
        {
            return null;
        }

        var match = DocFragmentRegex.Match(progress);
        if (!match.Success)
        {
            return null;
        }

        return int.TryParse(match.Groups[1].Value, out var chapterNumber)
            ? chapterNumber
            : null;
    }
}