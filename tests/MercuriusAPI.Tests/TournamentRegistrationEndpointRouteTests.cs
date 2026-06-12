using Mercurius.LAN.API.Endpoints;
using Mercurius.LAN.API.Services.GameServices;
using Mercurius.LAN.API.Services.RegistrationServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Mercurius.LAN.API.Tests;

public class TournamentRegistrationEndpointRouteTests
{
    [Theory]
    [InlineData("GET", "v{version:apiVersion}/lan/games/{gameId:guid}/registrations/me")]
    [InlineData("GET", "v{version:apiVersion}/lan/games/{gameId:guid}/registrations/eligibility/individual")]
    [InlineData("POST", "v{version:apiVersion}/lan/games/{gameId:guid}/registrations/individual")]
    [InlineData("PUT", "v{version:apiVersion}/lan/games/{gameId:guid}/registrations/teams/{teamId:guid}/roster")]
    [InlineData("POST", "v{version:apiVersion}/lan/games/{gameId:guid}/registrations/roster-confirmations/{rosterMemberId:guid}/confirm")]
    public void CurrentUserRegistrationRoutes_RequireAuthorization(string method, string routePattern)
    {
        var endpoint = GetRegistrationRouteEndpoint(method, routePattern);

        Assert.DoesNotContain(endpoint.Metadata, metadata => metadata is IAllowAnonymous);
        Assert.Contains(endpoint.Metadata, metadata => metadata is IAuthorizeData);
    }

    [Theory]
    [InlineData("GET", "v{version:apiVersion}/lan/games/{gameId:guid}/registrations/admin/")]
    [InlineData("DELETE", "v{version:apiVersion}/lan/games/{gameId:guid}/registrations/admin/users/{userId:guid}")]
    [InlineData("DELETE", "v{version:apiVersion}/lan/games/{gameId:guid}/registrations/admin/teams/{teamId:guid}")]
    public void AdminRegistrationRoutes_RequireAdminAuthorization(string method, string routePattern)
    {
        var endpoint = GetRegistrationRouteEndpoint(method, routePattern);
        var authorizeMetadata = endpoint.Metadata.OfType<AuthorizeAttribute>().ToList();

        Assert.DoesNotContain(endpoint.Metadata, metadata => metadata is IAllowAnonymous);
        Assert.Contains(authorizeMetadata, metadata => metadata.Roles == "admin");
    }

    [Theory]
    [InlineData("POST", "v{version:apiVersion}/lan/games/{id}/users")]
    [InlineData("DELETE", "v{version:apiVersion}/lan/games/{id}/users/{userId}")]
    [InlineData("POST", "v{version:apiVersion}/lan/games/{id}/teams")]
    [InlineData("DELETE", "v{version:apiVersion}/lan/games/{id}/teams/{teamId}")]
    public void LegacyGameParticipantMutationRoutes_AreRemoved(string method, string routePattern)
    {
        var endpoints = GetGameRouteEndpoints(method);

        Assert.DoesNotContain(endpoints, endpoint => endpoint.RoutePattern.RawText == routePattern);
    }

    private static RouteEndpoint GetRegistrationRouteEndpoint(string method, string routePattern)
    {
        return GetRegistrationRouteEndpoints(method)
            .Single(endpoint => endpoint.RoutePattern.RawText == routePattern);
    }

    private static IEnumerable<RouteEndpoint> GetRegistrationRouteEndpoints(string method)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddApiVersioning();
        builder.Services.AddScoped<ITournamentRegistrationService>(_ => throw new NotSupportedException());

        var app = builder.Build();
        app.MapTournamentRegistrationEndpoints();

        return ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.Metadata
                .OfType<IHttpMethodMetadata>()
                .Any(metadata => metadata.HttpMethods.Contains(method)));
    }

    private static IEnumerable<RouteEndpoint> GetGameRouteEndpoints(string method)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddApiVersioning();
        builder.Services.AddScoped<IGameService>(_ => throw new NotSupportedException());

        var app = builder.Build();
        app.MapGameEndpoints();

        return ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.Metadata
                .OfType<IHttpMethodMetadata>()
                .Any(metadata => metadata.HttpMethods.Contains(method)));
    }
}
