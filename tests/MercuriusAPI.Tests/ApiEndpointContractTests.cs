using Mercurius.LAN.API.Hubs;
using Mercurius.Modules.Competition;
using Mercurius.Modules.Competition.Application.Services;
using Mercurius.LAN.API.Data;
using Mercurius.Modules.Discovery;
using Mercurius.Modules.Discovery.Contracts;
using Mercurius.Modules.Identity;
using Mercurius.Modules.Identity.Services;
using Mercurius.Modules.Sponsorship;
using Mercurius.Modules.Teams;
using Mercurius.Modules.Teams.Services;
using Platform.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Mercurius.LAN.API.Tests;

public class ApiEndpointContractTests
{
    [Theory]
    [InlineData("GET", "v{version:apiVersion}/lan/games/", "Games")]
    [InlineData("GET", "v{version:apiVersion}/lan/games/{id}", "Games")]
    [InlineData("GET", "v{version:apiVersion}/lan/matches/{id}", "Matches")]
    [InlineData("GET", "v{version:apiVersion}/lan/sponsors/", "Sponsors")]
    [InlineData("GET", "v{version:apiVersion}/lan/sponsors/{id}", "Sponsors")]
    [InlineData("GET", "v{version:apiVersion}/lan/teams/", "Teams")]
    [InlineData("GET", "v{version:apiVersion}/lan/teams/{id:guid}", "Teams")]
    [InlineData("GET", "v{version:apiVersion}/lan/public/teams/{teamName}", "Public Teams")]
    [InlineData("GET", "v{version:apiVersion}/lan/public/users/{username}", "Users")]
    [InlineData("GET", "v{version:apiVersion}/lan/search/", "Search")]
    public void PublicReadRoutes_AllowAnonymousAndKeepTags(string method, string routePattern, string expectedTag)
    {
        var endpoint = GetEndpoint(method, routePattern);

        Assert.Contains(endpoint.Metadata, metadata => metadata is IAllowAnonymous);
        Assert.Contains(expectedTag, endpoint.Metadata.GetMetadata<ITagsMetadata>()?.Tags ?? []);
    }

    [Theory]
    [InlineData("GET", "v{version:apiVersion}/lan/users/me")]
    [InlineData("PUT", "v{version:apiVersion}/lan/users/me")]
    [InlineData("PATCH", "v{version:apiVersion}/lan/users/me")]
    [InlineData("POST", "v{version:apiVersion}/lan/users/me/complete-profile")]
    [InlineData("GET", "v{version:apiVersion}/lan/users/me/username-availability")]
    [InlineData("GET", "v{version:apiVersion}/lan/users/")]
    [InlineData("POST", "v{version:apiVersion}/lan/users/me/resend-verification-email")]
    [InlineData("POST", "v{version:apiVersion}/lan/users/me/password-reset")]
    [InlineData("DELETE", "v{version:apiVersion}/lan/users/me")]
    [InlineData("POST", "v{version:apiVersion}/lan/teams/")]
    [InlineData("GET", "v{version:apiVersion}/lan/teams/me/summary")]
    [InlineData("GET", "v{version:apiVersion}/lan/teams/me/invites")]
    [InlineData("GET", "v{version:apiVersion}/lan/teams/me/sent-invites")]
    [InlineData("POST", "v{version:apiVersion}/lan/teams/{id}/leave")]
    [InlineData("DELETE", "v{version:apiVersion}/lan/teams/{id:guid}/members/{userId:guid}")]
    [InlineData("DELETE", "v{version:apiVersion}/lan/teams/{id:guid}")]
    [InlineData("POST", "v{version:apiVersion}/lan/teams/{id}/invites/{userId}")]
    [InlineData("DELETE", "v{version:apiVersion}/lan/teams/{id}/invites/{inviteId}")]
    [InlineData("PUT", "v{version:apiVersion}/lan/teams/invites/{inviteId}")]
    [InlineData("PUT", "v{version:apiVersion}/lan/teams/{id}/captain")]
    [InlineData("POST", "v{version:apiVersion}/lan/teams/{id}/logo")]
    [InlineData("DELETE", "v{version:apiVersion}/lan/teams/{id}/logo")]
    [InlineData("GET", "v{version:apiVersion}/lan/games/{gameId:guid}/registrations/me")]
    [InlineData("GET", "v{version:apiVersion}/lan/games/{gameId:guid}/registrations/eligibility/individual")]
    [InlineData("GET", "v{version:apiVersion}/lan/games/{gameId:guid}/registrations/eligibility/teams/{teamId:guid}")]
    [InlineData("POST", "v{version:apiVersion}/lan/games/{gameId:guid}/registrations/eligibility/teams/{teamId:guid}/roster")]
    [InlineData("POST", "v{version:apiVersion}/lan/games/{gameId:guid}/registrations/individual")]
    [InlineData("DELETE", "v{version:apiVersion}/lan/games/{gameId:guid}/registrations/individual/me")]
    [InlineData("PUT", "v{version:apiVersion}/lan/games/{gameId:guid}/registrations/teams/{teamId:guid}/roster")]
    [InlineData("DELETE", "v{version:apiVersion}/lan/games/{gameId:guid}/registrations/teams/{teamId:guid}")]
    [InlineData("POST", "v{version:apiVersion}/lan/games/{gameId:guid}/registrations/roster-confirmations/{rosterMemberId:guid}/confirm")]
    public void AuthenticatedUserRoutes_RequireAuthorization(string method, string routePattern)
    {
        var endpoint = GetEndpoint(method, routePattern);

        AssertRequiresAuthorization(endpoint);
    }

    [Theory]
    [InlineData("POST", "v{version:apiVersion}/lan/games/")]
    [InlineData("PATCH", "v{version:apiVersion}/lan/games/{id}")]
    [InlineData("DELETE", "v{version:apiVersion}/lan/games/{id}")]
    [InlineData("PUT", "v{version:apiVersion}/lan/games/{id}/sponsors")]
    [InlineData("POST", "v{version:apiVersion}/lan/games/{id}/start")]
    [InlineData("POST", "v{version:apiVersion}/lan/games/{id}/reset")]
    [InlineData("POST", "v{version:apiVersion}/lan/games/{id}/complete")]
    [InlineData("POST", "v{version:apiVersion}/lan/games/{id}/cancel")]
    [InlineData("PUT", "v{version:apiVersion}/lan/matches/{id}")]
    [InlineData("POST", "v{version:apiVersion}/lan/sponsors/")]
    [InlineData("PATCH", "v{version:apiVersion}/lan/sponsors/{id}")]
    [InlineData("DELETE", "v{version:apiVersion}/lan/sponsors/{id}")]
    [InlineData("POST", "v{version:apiVersion}/lan/users/")]
    [InlineData("GET", "v{version:apiVersion}/lan/users/{id:guid}")]
    [InlineData("PATCH", "v{version:apiVersion}/lan/users/{id:guid}")]
    [InlineData("DELETE", "v{version:apiVersion}/lan/users/{id:guid}")]
    [InlineData("DELETE", "v{version:apiVersion}/lan/users/{username:nonguid}")]
    [InlineData("DELETE", "v{version:apiVersion}/lan/users/{username}/account")]
    [InlineData("GET", "v{version:apiVersion}/lan/games/{gameId:guid}/registrations/admin/")]
    [InlineData("DELETE", "v{version:apiVersion}/lan/games/{gameId:guid}/registrations/admin/users/{userId:guid}")]
    [InlineData("DELETE", "v{version:apiVersion}/lan/games/{gameId:guid}/registrations/admin/teams/{teamId:guid}")]
    public void AdminRoutes_RequireAdminAuthorization(string method, string routePattern)
    {
        var endpoint = GetEndpoint(method, routePattern);

        AssertRequiresAuthorization(endpoint);
        Assert.Contains(endpoint.Metadata.OfType<AuthorizeAttribute>(), metadata => metadata.Roles == "admin");
    }

    [Theory]
    [InlineData("POST", "/internal/discovery/search-index-rebuild-jobs/")]
    [InlineData("GET", "/internal/discovery/search-index-rebuild-jobs/{jobId:guid}")]
    public void DiscoveryRebuildRoutes_RequireAdminAuthorization(string method, string routePattern)
    {
        var endpoint = GetEndpoint(method, routePattern);

        AssertRequiresAuthorization(endpoint);
        Assert.Contains(endpoint.Metadata.OfType<AuthorizeAttribute>(), metadata => metadata.Roles == "admin");
    }

    [Theory]
    [InlineData("POST", "v{version:apiVersion}/lan/games/{id}/users")]
    [InlineData("DELETE", "v{version:apiVersion}/lan/games/{id}/users/{userId}")]
    [InlineData("POST", "v{version:apiVersion}/lan/games/{id}/teams")]
    [InlineData("DELETE", "v{version:apiVersion}/lan/games/{id}/teams/{teamId}")]
    [InlineData("DELETE", "v{version:apiVersion}/lan/teams/{id}/users/{userId}")]
    [InlineData("PUT", "v{version:apiVersion}/lan/teams/{id}")]
    [InlineData("POST", "v{version:apiVersion}/lan/teams/{id}/users/invite/{userId}")]
    [InlineData("GET", "v{version:apiVersion}/lan/players/")]
    public void RemovedOrUnavailableRoutes_StayUnavailable(string method, string routePattern)
    {
        var endpoints = GetEndpoints(method);

        Assert.DoesNotContain(endpoints, endpoint => endpoint.RoutePattern.RawText == routePattern);
    }

    [Fact]
    public void TeamManagementHub_RemainsMappedAndAuthenticated()
    {
        var endpoint = GetRouteEndpoint("/v1/lan/team-events");

        AssertRequiresAuthorization(endpoint);
    }

    private static void AssertRequiresAuthorization(RouteEndpoint endpoint)
    {
        Assert.DoesNotContain(endpoint.Metadata, metadata => metadata is IAllowAnonymous);
        Assert.Contains(endpoint.Metadata, metadata => metadata is IAuthorizeData);
    }

    private static RouteEndpoint GetEndpoint(string method, string routePattern)
    {
        return GetEndpoints(method)
            .Single(endpoint => endpoint.RoutePattern.RawText == routePattern);
    }

    private static RouteEndpoint GetRouteEndpoint(string routePattern)
    {
        return GetEndpoints()
            .Single(endpoint => endpoint.RoutePattern.RawText == routePattern);
    }

    private static IReadOnlyList<RouteEndpoint> GetEndpoints(string method)
    {
        return GetEndpoints()
            .Where(endpoint => endpoint.Metadata
                .OfType<IHttpMethodMetadata>()
                .Any(metadata => metadata.HttpMethods.Contains(method)))
            .ToList();
    }

    private static IReadOnlyList<RouteEndpoint> GetEndpoints()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddApiVersioning();
        builder.Services.AddRealtimeNotificationServices();
        RegisterEndpointServices(builder.Services);
        builder.Services.AddSponsorshipModule<MercuriusDBContext>(builder.Configuration);
        builder.Services.AddHttpConventions();

        var app = builder.Build();
        app.MapCompetitionModule();
        app.MapTeamsModule();
        app.MapSponsorshipModule();
        app.MapIdentityModule();
        app.MapDiscoveryModule();
        app.MapHub<TeamManagementHub>("/v1/lan/team-events").RequireAuthorization();

        return ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .ToList();
    }

    private static void RegisterEndpointServices(IServiceCollection services)
    {
        services.AddScoped<IGameService>(_ => throw new NotSupportedException());
        services.AddScoped<ITournamentRegistrationService>(_ => throw new NotSupportedException());
        services.AddScoped<IMatchService>(_ => throw new NotSupportedException());
        services.AddScoped<ITeamService>(_ => throw new NotSupportedException());
        services.AddScoped<IUserService>(_ => throw new NotSupportedException());
        services.AddScoped<IDiscoveryModule>(_ => throw new NotSupportedException());
    }
}
