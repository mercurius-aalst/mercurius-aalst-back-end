using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mercurius.Modules.Teams;

public static class TeamsModuleConfiguration
{
    public static IServiceCollection AddTeamsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapTeamsModule(this IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
