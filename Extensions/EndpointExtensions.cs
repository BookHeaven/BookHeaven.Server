using System.Reflection;
using BookHeaven.Server.Abstractions.Api;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BookHeaven.Server.Extensions;

public static class EndpointExtensions
{
    public static IServiceCollection AddEndpoints(this IServiceCollection services, Assembly assembly)
    {
        var serviceDescriptors = assembly
            .DefinedTypes
            .Where(type => type is { IsAbstract: false, IsInterface: false } &&
                           type.IsAssignableTo(typeof(IEndpoint)))
            .Select(type => ServiceDescriptor.Transient(typeof(IEndpoint), type))
            .ToArray();

        services.TryAddEnumerable(serviceDescriptors);

        return services;
    }
    
    public static IApplicationBuilder MapEndpoints(
        this WebApplication app)
    {
        var group = app.MapGroup("/api");
        
        var endpoints = app.Services
            .GetRequiredService<IEnumerable<IEndpoint>>();

        foreach (var endpoint in endpoints)
        {
            endpoint.MapEndpoint(group);
        }

        return app;
    }

}

