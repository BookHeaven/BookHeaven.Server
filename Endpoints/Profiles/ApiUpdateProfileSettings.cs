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
            app.MapPost("/profile/settings/update", ApiHandler);
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