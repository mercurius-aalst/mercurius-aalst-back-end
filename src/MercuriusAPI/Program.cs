using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.Configuration;
using Mercurius.LAN.API.Endpoints;
using Mercurius.LAN.API.Extensions;
using Mercurius.LAN.API.Hubs;
using Mercurius.LAN.API.Middleware;
using Mercurius.LAN.API.Options;
using Mercurius.LAN.API.Services.Auth0;
using Platform;
using Platform.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

namespace Mercurius.LAN.API;

public class Program
{
    private const string CorsPolicyName = "AllowMercuriusAalst";

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables("Mercurius.LAN.API_");

        builder.Services.AddDbContext<MercuriusDBContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("MercuriusDB")));

        builder.Services.AddValidation();
        builder.Services.AddVersionedSwagger(
            builder.Environment,
            documentTitle: "Mercurius API",
            includeXmlComments: true,
            useEnumSchemaFilter: true);
        builder.Services.AddApplicationServices();
        builder.Services.Configure<Auth0ManagementOptions>(builder.Configuration.GetSection(Auth0ManagementOptions.SectionName));
        builder.Services.AddHttpClient<IAuth0ManagementService, Auth0ManagementService>();
        builder.Services.AddApiProblemDetails<ApiExceptionHandler>();
        builder.Services.AddHttpConventions();
        builder.Services.AddAuth0JwtAuthentication(builder.Configuration.GetSection("Auth0"));
        builder.Services.AddRealtimeNotificationServices();
        var rateLimitingSection = builder.Configuration.GetSection("RateLimiting");
        builder.Services.AddFixedWindowRateLimiting(new FixedWindowRateLimitingOptions
        {
            GlobalPermitLimit = rateLimitingSection.GetValue("GlobalPermitLimit", 120),
            PolicyPermitLimit = rateLimitingSection.GetValue("SearchPermitLimit", 30),
            Window = TimeSpan.FromSeconds(rateLimitingSection.GetValue("WindowSeconds", 60)),
            UnconditionalPolicyName = RateLimitPolicies.AnonymousSearch,
            ConditionalPolicyName = RateLimitPolicies.AuthenticatedSearch,
            ConditionalQueryParameterName = "query"
        });
        builder.Services.AddWildcardSubdomainCors(
            CorsPolicyName,
            allowedOrigin: "https://*.mercurius-aalst.be");

        var app = builder.Build();
        app.UseCors(CorsPolicyName);
        app.ApplyMigrations<MercuriusDBContext>();
        app.UseApiExceptionHandling();
        app.UseImageflowWithCaching(
            requestPath: "/images",
            storagePath: app.Configuration["FileStorage:Location"],
            cacheControl: "public, max-age=31536000");
        app.UseSecurityPipeline();
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, "staticfiles")),
            RequestPath = "/staticfiles"
        });
        app.UseVersionedSwaggerUI(customJavascriptPath: "/staticfiles/swagger-custom.js");

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
