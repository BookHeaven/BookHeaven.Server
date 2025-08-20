using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Features.ProfileSettingss;
using BookHeaven.Server.Abstractions.Api;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BookHeaven.Server.Endpoints.Profiles;

public static class ApiUpdateProfileSettings
{
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPut("/profile/settings/update", ApiHandler)
                .WithName("UpdateProfileSettings")
                .WithSummary("Updates the profile settings for a given profile ID.")
                .WithDescription("This endpoint allows you to update the profile settings for a specific profile. " +
                                 "If the profile settings do not exist, they will be created.")
                .Produces(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .Accepts<ProfileSettings>("application/json")
                .WithTags("Profiles");
        }
        
        private static async Task<IResult> ApiHandler(
            [FromBody] ProfileSettings profileSettings,
            ISender sender,
            ILogger<ApiGetAllProfiles.Endpoint> logger)
        {
            var existingSettings = await sender.Send(new GetProfileSettings.Query(profileSettings.ProfileId));
            if (existingSettings.IsFailure)
            {
                await sender.Send(new AddProfileSettings.Command(profileSettings));
            }
            else
            {
                await sender.Send(new UpdateProfileSettings.Command(profileSettings));
            }

            return Results.Ok();
        }
        
    }
}