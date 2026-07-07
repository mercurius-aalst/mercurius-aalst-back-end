using Mercurius.LAN.API.Services.Files;
using Mercurius.LAN.API.Services.GameServices;
using Mercurius.LAN.API.Services.MatchServices;
using Mercurius.LAN.API.Services.MatchServices.BracketTypes;
using Mercurius.LAN.API.Services.RegistrationServices;
using Mercurius.LAN.API.Services.SearchServices;
using Mercurius.LAN.API.Services.SponsorServices;
using Mercurius.LAN.API.Services.TeamServices;
using Mercurius.Modules.Teams.Contracts;
using Mercurius.Modules.Teams.Services;

namespace Mercurius.LAN.API.Extensions;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddTransient<ITeamCompetitionReadService, EfTeamCompetitionReadService>();
        services.AddTransient<ITeamLogoStorage, FileTeamLogoStorage>();
        services.AddTransient<IGameService, GameService>();
        services.AddTransient<ITournamentRegistrationService, TournamentRegistrationService>();
        services.AddTransient<IMatchService, MatchService>();
        services.AddTransient<ISponsorService, SponsorService>();
        services.AddTransient<ISearchService, SearchService>();

        services.AddTransient<IFileService, FileService>();
        services.Decorate<IFileService, FileValidationService>();

        services.AddTransient<IMatchModeratorFactory, MatchModeratorFactory>();
        services.AddTransient<SingleEliminationMatchModerator>();
        services.AddTransient<DoubleEliminationMatchModerator>();
        services.AddTransient<RoundRobinMatchModerator>();

        return services;
    }
}
