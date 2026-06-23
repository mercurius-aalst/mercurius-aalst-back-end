using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mercurius.Modules.Media;

public static class MediaModuleConfiguration
{
    public static IServiceCollection AddMediaModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapMediaModule(this IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
