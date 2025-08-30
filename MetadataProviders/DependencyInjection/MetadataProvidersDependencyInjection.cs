using BookHeaven.Server.Abstractions;
using BookHeaven.Server.MetadataProviders.Abstractions;
using BookHeaven.Server.MetadataProviders.Factory;

namespace BookHeaven.Server.MetadataProviders.DependencyInjection;

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