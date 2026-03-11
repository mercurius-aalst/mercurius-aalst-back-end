using Asp.Versioning;
using Mercurius.LAN.API.Services.Auth;
using Mercurius.LAN.API.Services.Auth.Login;
using Mercurius.LAN.API.Services.Auth.Token;
using Mercurius.LAN.API.Services.Files;
using Mercurius.LAN.API.Services.GameServices;
using Mercurius.LAN.API.Services.MatchServices;
using Mercurius.LAN.API.Services.MatchServices.BracketTypes;
using Mercurius.LAN.API.Services.ParticipantServices;
using Mercurius.LAN.API.Services.PlayerServices;
using Mercurius.LAN.API.Services.SponsorServices;
using Mercurius.LAN.API.Services.TeamServices;
using Mercurius.LAN.API.Services.UserServices;
using Microsoft.Extensions.FileProviders;

namespace Mercurius.LAN.API.Extensions;

public static class DepedencyConfiguration
{
    public static IServiceCollection ConfigureVersionedSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

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

    public static void UseSecuredSwaggerUI(this WebApplication app)
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, "staticfiles")),
            RequestPath = "/staticfiles"
        })
            .UseSwagger()
            .UseSwaggerUI(options =>
            {
                options.InjectJavascript("/staticfiles/swagger-custom.js");
            });
    }

    public static IServiceCollection AddServiceDependencies(this IServiceCollection services)
    {
        services.AddTransient<IPlayerService, PlayerService>();
        services.AddTransient<ITeamService, TeamService>();
        services.AddTransient<IGameService, GameService>();
        services.AddTransient<IMatchService, MatchService>();
        services.AddTransient<IParticipantService, ParticipantService>();
        services.AddTransient<ISponsorService, SponsorService>();

        services.AddTransient<IUserService, UserService>();
        services.Decorate<IUserService, UserValidationService>();

        services.AddTransient<IFileService, FileService>();
        services.Decorate<IFileService, FileValidationService>();

        services.AddSingleton<ITokenService, TokenService>();

        services.AddSingleton<ILoginAttemptService>(provider =>
            new LoginAttemptService(5, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5)));
        services.AddSingleton<TokenService>();

        services.AddTransient<IAuthService, AuthService>();
        services.Decorate<IAuthService, AuthValidationService>();

        services.AddTransient<IMatchModeratorFactory, MatchModeratorFactory>();
        services.AddTransient<SingleEliminationMatchModerator>();
        services.AddTransient<DoubleEliminationMatchModerator>();
        services.AddTransient<SwissStageMatchModerator>();
        services.AddTransient<RoundRobinMatchModerator>();
        return services;
    }
}
