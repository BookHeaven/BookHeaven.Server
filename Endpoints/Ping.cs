using BookHeaven.Server.Abstractions.Api;

namespace BookHeaven.Server.Endpoints;

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