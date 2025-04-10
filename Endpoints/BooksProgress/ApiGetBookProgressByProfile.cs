using BookHeaven.Domain.Features.BooksProgress;
using BookHeaven.Server.Abstractions.Api;
using MediatR;

namespace BookHeaven.Server.Endpoints.BooksProgress;

public static class ApiGetBookProgressByProfile
{
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/profiles/{profileId:guid}/{bookId:guid}", ApiHandler);
        }
        
        private static async Task<IResult> ApiHandler(
            Guid bookId,
            Guid profileId,
            ISender sender,
            ILogger<Endpoint> logger)
        {
            var getProgress = await sender.Send(new GetBookProgressByProfile.Query(bookId, profileId));
            if (getProgress.IsSuccess)
            {
                return Results.Ok(getProgress.Value);
            }
            logger.LogError(getProgress.Error.Description);
            return Results.Problem(getProgress.Error.Description);
        }
    }
}