using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mercurius.Modules.Teams.Api;

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
        endpoints.MapTeamEndpoints();
        return endpoints;
    }
}
