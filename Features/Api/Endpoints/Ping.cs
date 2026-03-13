using BookHeaven.Server.Features.Api.Abstractions;

namespace BookHeaven.Server.Features.Api.Endpoints;

public static class Ping
{
    public class Endpoint : IEndpoint
    {
        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapGet("/ping", () => Results.Ok()).ExcludeFromDescription();
        }
    }
}