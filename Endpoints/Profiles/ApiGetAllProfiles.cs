using BookHeaven.Domain.Features.Profiles;
using BookHeaven.Server.Abstractions.Api;
using MediatR;

namespace BookHeaven.Server.Endpoints.Profiles;

public static class ApiGetAllProfiles
{
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/profiles", ApiHandler);
        }
        
        private static async Task<IResult> ApiHandler(
            ISender sender,
            ILogger<Endpoint> logger)
        {
            var getProfiles = await sender.Send(new GetAllProfiles.Query(true));
            if (getProfiles.IsSuccess)
            {
                return Results.Ok(getProfiles.Value);
            }
            logger.LogError(getProfiles.Error.Description);
            return Results.Problem(getProfiles.Error.Description);
        }
        
    }
}