using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Routing;

namespace Platform.Extensions;

public static class HttpExtensions
{
    public static IServiceCollection AddHttpConventions(this IServiceCollection services)
    {
        services.Configure<RouteOptions>(options =>
        {
            options.ConstraintMap["nonguid"] = typeof(NonGuidRouteConstraint);
        });

        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        return services;
    }
}
