using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Mercurius.Platform.Swagger;

internal sealed class EnumSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema model, SchemaFilterContext context)
    {
        if (!context.Type.IsEnum)
            return;

        model.Enum.Clear();
        foreach (var name in Enum.GetNames(context.Type))
            model.Enum.Add(new OpenApiString(name));
    }
}
