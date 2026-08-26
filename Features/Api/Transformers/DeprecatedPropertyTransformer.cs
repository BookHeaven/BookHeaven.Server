using System.Reflection;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace BookHeaven.Server.Features.Api.Transformers;

public static class DeprecatedPropertyTransformer
{
    public static void AddDeprecatedPropertyTransformer(this OpenApiOptions options)
    {
        
        options.AddSchemaTransformer((schema, context, ct) =>
        {
            if(schema.Properties is null || schema.Properties.Count == 0) return Task.CompletedTask;
            
            var properties = context.JsonTypeInfo.Type.GetProperties();
            
            if(!properties.Any(p => p.GetCustomAttribute<ObsoleteAttribute>() is not null)) return Task.CompletedTask;

            foreach (var property in schema.Properties!)
            {
                var member = properties.FirstOrDefault(p =>
                    string.Equals(
                        p.Name,
                        property.Key,
                        StringComparison.OrdinalIgnoreCase));

                if (member?.GetCustomAttribute<ObsoleteAttribute>() is not null &&
                    property.Value is OpenApiSchema propertySchema)
                {
                    propertySchema.Deprecated = true;
                }
            }

            return Task.CompletedTask;
        });
    }
}