namespace BookHeaven.Server.Abstractions.Api;

public interface IEndpoint
{
    void MapEndpoint(IEndpointRouteBuilder app);
}