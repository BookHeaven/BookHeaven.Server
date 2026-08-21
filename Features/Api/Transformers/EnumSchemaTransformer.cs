using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace BookHeaven.Server.Features.Api.Transformers;

public static class EnumSchemaTransformer
{
    public static void AddEnumSchemaTransformer(this OpenApiOptions options)
    {
        options.AddSchemaTransformer((schema, context, ct) =>
        {
            if (!context.JsonTypeInfo.Type.IsEnum) return Task.CompletedTask;

            List<JsonNode> values = [];
            var names = new JsonArray();
            // Must exclude values with the [JsonIgnore] attribute
            foreach (var name in context.JsonTypeInfo.Type.GetEnumNames())
            {
                var member = context.JsonTypeInfo.Type.GetMember(name).FirstOrDefault();
                if (member != null && member.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                {
                    continue;
                }
                var value = Convert.ToInt32(Enum.Parse(context.JsonTypeInfo.Type, name));
                values.Add(JsonValue.Create(value));
                names.Add(name);
            }
		
            schema.Enum = values;
            schema.AddExtension("x-enum-varnames", new JsonNodeExtension(names));
            return Task.CompletedTask;
        });
    }
}