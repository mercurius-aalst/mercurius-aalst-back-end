using System.Text.Json.Serialization;
using Mercurius.Platform.Routing;
using Microsoft.AspNetCore.Routing;

namespace Mercurius.Platform.Http;

public static class MercuriusHttpExtensions
{
    public static IServiceCollection AddMercuriusHttpConventions(this IServiceCollection services)
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
