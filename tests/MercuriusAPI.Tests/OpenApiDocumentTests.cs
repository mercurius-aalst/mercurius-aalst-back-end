using System.Security.Claims;
using Mercurius.LAN.API.Endpoints;
using Mercurius.LAN.API.Hubs;
using Mercurius.LAN.API.Services.GameServices;
using Mercurius.LAN.API.Services.MatchServices;
using Mercurius.LAN.API.Services.RegistrationServices;
using Mercurius.LAN.API.Services.SearchServices;
using Mercurius.LAN.API.Services.SponsorServices;
using Mercurius.LAN.API.Services.UserServices;
using Mercurius.Modules.Teams;
using Mercurius.Modules.Teams.Services;
using Platform.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;

namespace Mercurius.LAN.API.Tests;

public class OpenApiDocumentTests
{
    [Fact]
    public async Task SwaggerDocument_GeneratesV1DocumentWithRepresentativePaths()
    {
        using var app = CreateSwaggerApp();
        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync();

        try
        {
            var swaggerProvider = app.Services.GetRequiredService<ISwaggerProvider>();
            var endpointCount = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).Count();
            var apiDescriptionCount = app.Services.GetRequiredService<IApiDescriptionGroupCollectionProvider>()
                .ApiDescriptionGroups.Items.Sum(group => group.Items.Count);

            var document = swaggerProvider.GetSwagger("v1");

            Assert.True(endpointCount > 0, "The Swagger test host should have mapped endpoints.");
            Assert.True(apiDescriptionCount > 0, "ApiExplorer should discover the mapped endpoints.");
            Assert.Equal("Mercurius API", document.Info.Title);
            AssertPathHasOperation(document, "/v1/lan/games", OperationType.Get);
            AssertPathHasOperation(document, "/v1/lan/games/{id}", OperationType.Get);
            AssertPathHasOperation(document, "/v1/lan/games/{id}", OperationType.Patch);
            AssertPathHasOperation(document, "/v1/lan/teams", OperationType.Get);
            AssertPathHasOperation(document, "/v1/lan/teams/{id}", OperationType.Delete);
            AssertPathHasOperation(document, "/v1/lan/public/teams/{teamName}", OperationType.Get);
            AssertPathHasOperation(document, "/v1/lan/users/me", OperationType.Get);
            AssertPathHasOperation(document, "/v1/lan/public/users/{username}", OperationType.Get);
            AssertPathHasOperation(document, "/v1/lan/games/{gameId}/registrations/me", OperationType.Get);
            AssertPathHasOperation(document, "/v1/lan/games/{gameId}/registrations/admin", OperationType.Get);
            AssertPathHasOperation(document, "/v1/lan/search", OperationType.Get);
            AssertPathHasOperation(document, "/v1/lan/sponsors", OperationType.Post);
            AssertPathHasOperation(document, "/v1/lan/matches/{id}", OperationType.Put);
            Assert.DoesNotContain(document.Paths.Keys, path => path.StartsWith("/v2/", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            await app.StopAsync();
        }
    }

    private static void AssertPathHasOperation(OpenApiDocument document, string path, OperationType operation)
    {
        var matchingPath = document.Paths.Keys.SingleOrDefault(candidate =>
            string.Equals(candidate.TrimEnd('/'), path, StringComparison.Ordinal));

        Assert.True(matchingPath is not null, $"Missing {path}. Available paths: {string.Join(", ", document.Paths.Keys.OrderBy(key => key, StringComparer.Ordinal))}");
        Assert.True(document.Paths[matchingPath].Operations.ContainsKey(operation), $"{path} should expose {operation}.");
    }

    private static WebApplication CreateSwaggerApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddVersionedSwagger(
            builder.Environment,
            documentTitle: "Mercurius API",
            includeXmlComments: false,
            useEnumSchemaFilter: true);
        builder.Services.AddAuthorization();
        builder.Services.AddRealtimeNotificationServices();
        RegisterEndpointServices(builder.Services);
        builder.Services.AddHttpConventions();

        var app = builder.Build();
        app.Services.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "auth0|swagger-test"),
                new Claim(ClaimTypes.Role, "admin")
            ], "SwaggerTest"))
        };

        app.MapGameEndpoints();
        app.MapTournamentRegistrationEndpoints();
        app.MapMatchEndpoints();
        app.MapTeamsModule();
        app.MapSponsorEndpoints();
        app.MapUserEndpoints();
        app.MapSearchEndpoints();
        app.MapHub<TeamManagementHub>("/v1/lan/team-events").RequireAuthorization();

        return app;
    }

    private static void RegisterEndpointServices(IServiceCollection services)
    {
        services.AddScoped<IGameService>(_ => throw new NotSupportedException());
        services.AddScoped<ITournamentRegistrationService>(_ => throw new NotSupportedException());
        services.AddScoped<IMatchService>(_ => throw new NotSupportedException());
        services.AddScoped<ITeamsEndpointService>(_ => throw new NotSupportedException());
        services.AddScoped<ISponsorService>(_ => throw new NotSupportedException());
        services.AddScoped<IUserService>(_ => throw new NotSupportedException());
        services.AddScoped<ISearchService>(_ => throw new NotSupportedException());
    }
}
