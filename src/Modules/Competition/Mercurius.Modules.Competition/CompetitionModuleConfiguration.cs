using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Mercurius.Modules.Competition;

public static class CompetitionModuleConfiguration
{
    public static IServiceCollection AddCompetitionModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services;
    }

    public static IEndpointRouteBuilder MapCompetitionModule(this IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
