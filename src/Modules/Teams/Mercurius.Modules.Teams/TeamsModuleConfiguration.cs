using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mercurius.Modules.Teams.Contracts;
using Mercurius.Modules.Teams.Endpoints;
using Mercurius.Modules.Teams.Infrastructure;
using Mercurius.Modules.Teams.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mercurius.Modules.Teams;

public static class TeamsModuleConfiguration
{
    public static IServiceCollection AddTeamsModule<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TDbContext : DbContext
    {
        services.TryAddScoped<ITeamsDbContext, TeamsDbContextAdapter<TDbContext>>();
        services.AddTransient<ITeamsModule, TeamsModuleFacade>();
        services.AddTransient<TeamService>();
        services.AddTransient<TeamEventPublishingDecorator>();
        services.AddTransient<ITeamEndpointService, TeamEndpointService>();
        services.AddTransient<ITeamEventPublisher, RealtimeTeamEventPublisher>();
        services.AddTransient<Contracts.ITeamRealtimeAuthorizer, EfTeamRealtimeAuthorizer>();

        return services;
    }

    public static ModelBuilder ApplyTeamsModelConfiguration(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TeamConfiguration());
        modelBuilder.ApplyConfiguration(new TeamInviteConfiguration());

        return modelBuilder;
    }

    public static IEndpointRouteBuilder MapTeamsModule(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapTeamEndpoints();
        return endpoints;
    }
}
