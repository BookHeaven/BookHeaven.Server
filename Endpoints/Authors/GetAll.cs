using BookHeaven.Domain.Features.Authors;
using BookHeaven.Server.Abstractions.Api;
using MediatR;

namespace BookHeaven.Server.Endpoints.Authors;

public static class GetAll
{
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/authors", ApiHandler);
        }

        private static async Task<IResult> ApiHandler(
            ISender sender,
            ILogger<Endpoint> logger)
        {
            var getAuthors = await sender.Send(new GetAllAuthors.Query());
            if (getAuthors.IsSuccess)
            {
                return Results.Ok(getAuthors.Value);
            }
            logger.LogError(getAuthors.Error.Description);
            return Results.Problem(getAuthors.Error.Description);
        }
    }
}