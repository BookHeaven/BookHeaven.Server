using BookHeaven.Core.Entities.Base;
using BookHeaven.Core.Features.Books;
using BookHeaven.Core.Features.Collections;
using BookHeaven.Server.Features.Api.Abstractions;

namespace BookHeaven.Server.Features.Api.Endpoints.Collections;

public static class ApiGetCollections
{
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/collections", ApiHandler)
                .WithName("GetCollections")
                .WithTags("Collections")
                .Produces<List<Collection>>()
                .ProducesProblem(StatusCodes.Status500InternalServerError)
                .WithSummary("Get list of collections.")
                .WithDescription("Retrieves a list of all collections.");
        }
        
        private static async Task<IResult> ApiHandler(
            ISender sender,
            ILogger<Endpoint> logger)
        {
            var getCollections = await sender.Send(new GetAllCollections.Query());
            
            if (getCollections.IsFailure)
            {
                logger.LogError(getCollections.Error.Description);
                return Results.Problem(getCollections.Error.Description);
            }
            
            return Results.Ok(getCollections.Value);
        }
    }
}