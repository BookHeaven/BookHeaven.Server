using BookHeaven.Server.Features.Metadata.Abstractions;
using BookHeaven.Server.Features.Metadata.Factories;

namespace BookHeaven.Server.Features.Metadata.DependencyInjection;

public static class MetadataProvidersDependencyInjection
{
    public static IServiceCollection AddMetadataProviders(this IServiceCollection services)
    {
        services.AddTransient<MetadataProvidersFactory>();
        
        var metadataProviderTypes = typeof(MetadataProvidersDependencyInjection).Assembly.GetTypes()
            .Where(t => typeof(IMetadataProvider).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false });
        foreach (var type in metadataProviderTypes)
        {
            services.AddTransient(type);
        }
        return services;
    }
}