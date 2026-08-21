using Microsoft.AspNetCore.OpenApi;

namespace BookHeaven.Server.Features.Api.Transformers;

public static class DeprecatedOperationTransformer
{
    public static void AddDeprecatedOperationTransformer(this OpenApiOptions options)
    {
        options.AddOperationTransformer((operation, context, ct) =>
        {
            operation.Deprecated = context.Description.ActionDescriptor.EndpointMetadata
                .OfType<DeprecatedEndpointMetadata>()
                .FirstOrDefault() is not null;

            return Task.CompletedTask;
        });
    }
}