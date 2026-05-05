using Auth.Module.Persistence;
using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.Services.Files;
using Mercurius.LAN.API.Services.GameServices;
using Mercurius.LAN.API.Services.MatchServices;
using Mercurius.LAN.API.Services.MatchServices.BracketTypes;
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
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(new UrlSegmentApiVersionReader());
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });
        services.ConfigureOptions<ConfigureSwaggerOptions>();
        return services;
    }

    public static void UseSecuredSwaggerUI(this WebApplication app)
    {
        var apiVersionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, "staticfiles")),
            RequestPath = "/staticfiles"
        })
            .UseSwagger()
            .UseSwaggerUI(options =>
            {
                foreach (var description in apiVersionProvider.ApiVersionDescriptions)
                {
                    options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
                }

                options.InjectJavascript("/staticfiles/swagger-custom.js");
            });
    }

    public static IServiceCollection AddServiceDependencies(this IServiceCollection services)
    {
        services.AddScoped<UserProfileStore>();
        services.AddScoped<IExternalUserProfileProvisioner, ExternalUserProfileProvisioner>();
        services.AddTransient<IUserService, UserService>();
        services.Decorate<IUserService, UserValidationService>();

        services.AddTransient<ITeamService, TeamService>();
        services.AddTransient<IGameService, GameService>();
        services.AddTransient<IMatchService, MatchService>();
        services.AddTransient<ISponsorService, SponsorService>();

        services.AddTransient<IFileService, FileService>();
        services.Decorate<IFileService, FileValidationService>();


        services.AddTransient<IMatchModeratorFactory, MatchModeratorFactory>();
        services.AddTransient<SingleEliminationMatchModerator>();
        services.AddTransient<DoubleEliminationMatchModerator>();
        services.AddTransient<SwissStageMatchModerator>();
        services.AddTransient<RoundRobinMatchModerator>();
        return services;
    }
}
