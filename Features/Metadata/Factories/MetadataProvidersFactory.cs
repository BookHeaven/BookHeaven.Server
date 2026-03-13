using BookHeaven.Domain.Extensions;
using BookHeaven.Server.Features.Metadata.Abstractions;
using BookHeaven.Server.Features.Metadata.Services;

namespace BookHeaven.Server.Features.Metadata.Factories;

public class MetadataProvidersFactory(IServiceProvider serviceProvider)
{
    public enum MetadataProvider
    {
        [StringValue(nameof(GoogleBooksProvider))]
        GoogleBooks,
        [StringValue(nameof(OpenLibraryProvider))]
        OpenLibrary,
    }
    
    public IMetadataProvider? GetMetadataProvider(MetadataProvider metadataProvider)
    {
        // Find type in assembly by enum description
        var type = typeof(MetadataProvidersFactory).Assembly.GetTypes()
            .FirstOrDefault(t => typeof(IMetadataProvider).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false } &&
                                 t.Name.Equals(metadataProvider.StringValue(), StringComparison.OrdinalIgnoreCase));
        
        if (type == null) return null;
        return serviceProvider.GetService(type) as IMetadataProvider;
    }
}