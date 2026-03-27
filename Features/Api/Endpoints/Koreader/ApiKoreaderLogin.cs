using BookHeaven.Domain.Features.Profiles;
using BookHeaven.Server.Features.Api.Abstractions;

namespace BookHeaven.Server.Features.Api.Endpoints.Koreader;

public static class ApiKoreaderLogin
{
    public class Endpoint : IKoreaderSyncEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/users/auth", Handler)
                .ExcludeFromDescription();
        }

        private static async Task<IResult> Handler(
            ISender sender,
            IHttpContextAccessor httpContextAccessor)
        {
            var profileName = httpContextAccessor.HttpContext?.Request.Headers["x-auth-user"].ToString();
            
            if (string.IsNullOrEmpty(profileName))
            {
                return Results.BadRequest("Missing x-auth-user header");
            }
            
            var getProfile = await sender.Send(new GetProfileByName.Query(profileName));
            if (getProfile.IsFailure)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new
            {
                username = profileName
            });
        }
    }
}