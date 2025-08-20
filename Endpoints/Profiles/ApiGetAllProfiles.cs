using BookHeaven.Domain.Entities;
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
            app.MapGet("/profiles", ApiHandler)
                .WithName("GetAllProfiles")
                .WithTags("Profiles")
                .Produces<List<Profile>>()
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .WithSummary("Get all profiles")
                .WithDescription("Returns a list of all profiles in the system.");
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