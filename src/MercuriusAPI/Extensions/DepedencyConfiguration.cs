using Asp.Versioning;
using MercuriusAPI.Services.LAN.GameServices;
using MercuriusAPI.Services.LAN.MatchServices.BracketTypes;
using MercuriusAPI.Services.LAN.MatchServices;
using MercuriusAPI.Services.LAN.ParticipantServices;
using MercuriusAPI.Services.LAN.PlayerServices;
using MercuriusAPI.Services.LAN.TeamServices;
using System.Reflection;

namespace MercuriusAPI.Extensions
{
    public static class DepedencyConfiguration
    {
        public static IServiceCollection ConfigureVersionedSwagger(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory,
                    $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
            });
            services.AddApiVersioning(opt =>
            {
                opt.DefaultApiVersion = new ApiVersion(1, 0);
                opt.AssumeDefaultVersionWhenUnspecified = true;
                opt.ReportApiVersions = true;
                opt.ApiVersionReader = ApiVersionReader.Combine(new UrlSegmentApiVersionReader());
            });
            services.AddApiVersioning().AddApiExplorer(setup =>
            {
                setup.GroupNameFormat = "'v'VVV";
                setup.SubstituteApiVersionInUrl = true;
            });
            services.ConfigureOptions<ConfigureSwaggerOptions>();
            return services;
        }

        public static IServiceCollection AddServiceDependencies(this IServiceCollection services)
        {
            services.AddTransient<IPlayerService, PlayerService>();
            services.AddTransient<ITeamService, TeamService>();
            services.AddTransient<IGameService, GameService>();
            services.AddTransient<IParticipantService, ParticipantService>();

            services.AddTransient<IMatchModeratorFactory, MatchModeratorFactory>();
            services.AddTransient<SingleEliminationMatchModerator>();
            services.AddTransient<DoubleEliminationMatchModerator>();
            services.AddTransient<SwissStageMatchModerator>();
            services.AddTransient<RoundRobinMatchModerator>();
            return services;
        }
    }
}
