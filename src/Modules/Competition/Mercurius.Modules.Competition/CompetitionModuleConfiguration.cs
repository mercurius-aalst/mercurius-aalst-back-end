using Mercurius.Modules.Competition.Application;
using Mercurius.Modules.Competition.Application.Services;
using Mercurius.Modules.Competition.Application.Services.BracketTypes;
using Mercurius.Modules.Competition.Contracts;
using Mercurius.Modules.Competition.Endpoints;
using Mercurius.Modules.Competition.Infrastructure;
using Mercurius.Modules.Teams.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mercurius.Modules.Competition;

public static class CompetitionModuleConfiguration
{
    public static IServiceCollection AddCompetitionModule<TDbContext>(
        this IServiceCollection services,
        IConfiguration _)
        where TDbContext : DbContext
    {
        services.TryAddScoped<ICompetitionDbContext, CompetitionDbContextAdapter<TDbContext>>();
        services.AddTransient<ICompetitionModule, CompetitionModuleFacade>();
        services.AddTransient<CompetitionEligibilityEvaluator>();
        services.AddTransient<CompetitionDtoMapper>();
        services.AddTransient<RegistrationMappingContextBuilder>();
        services.AddTransient<TournamentRegistrationPersistenceCoordinator>();
        services.AddTransient<TournamentRegistrationReadModelService>();
        services.AddTransient<IGameService, GameService>();
        services.AddTransient<IMatchService, MatchService>();
        services.AddTransient<ITournamentRegistrationService, TournamentRegistrationService>();
        services.AddTransient<IMatchModeratorFactory, MatchModeratorFactory>();
        services.AddTransient<SingleEliminationMatchModerator>();
        services.AddTransient<DoubleEliminationMatchModerator>();
        services.AddTransient<RoundRobinMatchModerator>();
        services.AddTransient<ICompetitionRealtimePublisher, CompetitionRealtimePublisher>();
        services.AddTransient<ITeamCompetitionReadService, CompetitionTeamReadService>();

        return services;
    }

    public static ModelBuilder ApplyCompetitionModelConfiguration<TUser, TTeam, TGameSponsorPlacement>(
        this ModelBuilder modelBuilder)
        where TUser : class
        where TTeam : class
        where TGameSponsorPlacement : class
    {
        return modelBuilder.ApplyCompetitionConfiguration<TUser, TTeam, TGameSponsorPlacement>();
    }

    public static IEndpointRouteBuilder MapCompetitionModule(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGameEndpoints();
        endpoints.MapTournamentRegistrationEndpoints();
        endpoints.MapMatchEndpoints();

        return endpoints;
    }
}
