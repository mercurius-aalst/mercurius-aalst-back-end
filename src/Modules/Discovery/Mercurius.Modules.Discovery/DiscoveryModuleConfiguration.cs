using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mercurius.Modules.Discovery;

public static class DiscoveryModuleConfiguration
{
    public static IServiceCollection AddDiscoveryModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapDiscoveryModule(this IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
