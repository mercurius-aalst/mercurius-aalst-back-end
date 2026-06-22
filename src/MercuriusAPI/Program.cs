using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.Endpoints;
using Mercurius.LAN.API.Extensions;
using Mercurius.LAN.API.Hubs;
using Mercurius.LAN.API.Middleware;
using Mercurius.LAN.API.Options;
using Mercurius.LAN.API.Services.Auth0;
using Mercurius.Platform.Authentication;
using Mercurius.Platform.Cors;
using Mercurius.Platform.Http;
using Mercurius.Platform.Images;
using Mercurius.Platform.Migrations;
using Mercurius.Platform.ProblemDetails;
using Mercurius.Platform.RateLimiting;
using Mercurius.Platform.Realtime;
using Mercurius.Platform.Security;
using Mercurius.Platform.Swagger;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.LAN.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables("Mercurius.LAN.API_");

        builder.Services.AddDbContext<MercuriusDBContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("MercuriusDB")));

        builder.Services.AddValidation();
        builder.Services.AddMercuriusSwagger(
            builder.Environment,
            includeXmlComments: true,
            useEnumSchemaFilter: true);
        builder.Services.AddApplicationServices();
        builder.Services.Configure<Auth0ManagementOptions>(builder.Configuration.GetSection(Auth0ManagementOptions.SectionName));
        builder.Services.AddHttpClient<IAuth0ManagementService, Auth0ManagementService>();
        builder.Services.AddMercuriusProblemDetails<ApiExceptionHandler>();
        builder.Services.AddMercuriusHttpConventions();
        builder.Services.AddMercuriusAuthentication(builder.Configuration);
        builder.Services.AddMercuriusRealtime();
        builder.Services.AddMercuriusRateLimiting(builder.Configuration);
        builder.Services.AddMercuriusCors();

        var app = builder.Build();
        app.UseMercuriusCors();
        app.ApplyMercuriusMigrations<MercuriusDBContext>();
        app.UseMercuriusExceptionHandling();
        app.UseMercuriusImages();
        app.UseMercuriusSecurityPipeline();
        app.UseMercuriusSwaggerUI();

        app.MapGameEndpoints();
        app.MapTournamentRegistrationEndpoints();
        app.MapMatchEndpoints();
        app.MapTeamEndpoints();
        app.MapSponsorEndpoints();
        app.MapUserEndpoints();
        app.MapSearchEndpoints();
        app.MapHub<TeamManagementHub>("/v1/lan/team-events").RequireAuthorization();

        app.Run();
    }
}
