namespace BookHeaven.Server.Features.Api;

public static class RouteHandlerBuilderExtensions
{
    public static RouteHandlerBuilder IsDeprecated(this RouteHandlerBuilder builder)
    {
        return builder.WithMetadata(new DeprecatedEndpointMetadata());
    }
}