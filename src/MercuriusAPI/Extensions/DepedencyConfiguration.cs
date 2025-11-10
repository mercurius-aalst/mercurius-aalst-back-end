using Asp.Versioning;
using MercuriusAPI.Services.Auth;
using MercuriusAPI.Services.Auth.Login;
using MercuriusAPI.Services.Auth.Token;
using MercuriusAPI.Services.Files;
using MercuriusAPI.Services.LAN.GameServices;
using MercuriusAPI.Services.LAN.MatchServices;
using MercuriusAPI.Services.LAN.MatchServices.BracketTypes;
using MercuriusAPI.Services.LAN.ParticipantServices;
using MercuriusAPI.Services.LAN.PlayerServices;
using MercuriusAPI.Services.LAN.SponsorServices;
using MercuriusAPI.Services.LAN.TeamServices;
using MercuriusAPI.Services.UserServices;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace MercuriusAPI.Extensions;

public static class DepedencyConfiguration
{
    public static IServiceCollection ConfigureVersionedSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory,
                $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] {}
                }
            });
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
