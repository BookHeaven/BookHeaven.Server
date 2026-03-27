using BookHeaven.Domain.Features.KoreaderProgress;
using BookHeaven.Domain.Features.Profiles;
using BookHeaven.Server.Features.Api.Abstractions;

namespace BookHeaven.Server.Features.Api.Endpoints.Koreader;

public static class ApiGetKoreaderProgress
{
    public class Endpoint : IKoreaderSyncEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/syncs/progress/{documentHash}", Handler)
                .ExcludeFromDescription();
        }

        private static async Task<IResult> Handler(
            string documentHash,
            ISender sender,
            IHttpContextAccessor httpContextAccessor,
            ILogger<Endpoint> logger)
        {
            var profileName = httpContextAccessor.HttpContext?.Request.Headers["x-auth-user"].ToString();
            
            var getProfile = await sender.Send(new GetProfileByName.Query(profileName ?? ""));
            if (getProfile.IsFailure)
            {
                logger.LogWarning("Profile with name {ProfileName} not found.", profileName);
                return Results.Unauthorized();
            }
            
            var getProgress = await sender.Send(new GetKoreaderProgress.Query(getProfile.Value.ProfileId, documentHash));
            if (getProgress.IsFailure)
            {
                logger.LogWarning("Failed to get KOReader progress for hash '{DocumentHash}'.", documentHash);
                return Results.NotFound();
            }
            
            var progress = getProgress.Value;
            
            return Results.Ok(new
            {
                device = progress.DeviceName,
                device_id = progress.DeviceId,
                document = progress.DocumentHash,
                percentage = progress.Percentage,
                progress = progress.Progress,
                timestamp = new DateTimeOffset(progress.Timestamp).ToUnixTimeSeconds()
            });
        }
    }
}