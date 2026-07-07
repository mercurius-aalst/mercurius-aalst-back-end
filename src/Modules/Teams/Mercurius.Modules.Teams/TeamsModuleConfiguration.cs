using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mercurius.Modules.Teams.Contracts;
using Mercurius.Modules.Teams.Endpoints;
using Mercurius.Modules.Teams.Infrastructure;
using Mercurius.Modules.Teams.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mercurius.Modules.Teams;

public static class TeamsModuleConfiguration
{
    public static IServiceCollection AddTeamsModule<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TDbContext : class, ITeamsDbContext
    {
        services.TryAddScoped<ITeamsDbContext>(provider => provider.GetRequiredService<TDbContext>());
        services.TryAddTransient<ITeamCompetitionReadService, NullTeamCompetitionReadService>();
        services.AddTransient<ITeamsModule, TeamsModuleFacade>();
        services.AddTransient<ITeamLogoStorage, TeamLogoStorage>();
        services.AddTransient<ITeamService, TeamService>();
        services.Decorate<ITeamService, TeamEventPublishingDecorator>();
        services.AddTransient<ITeamEndpointService, TeamEndpointService>();
        services.AddTransient<ITeamEventPublisher, RealtimeTeamEventPublisher>();
        services.AddTransient<Contracts.ITeamRealtimeAuthorizer, EfTeamRealtimeAuthorizer>();

        return services;
    }

    public static IEndpointRouteBuilder MapTeamsModule(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapTeamEndpoints();
        return endpoints;
    }
}
