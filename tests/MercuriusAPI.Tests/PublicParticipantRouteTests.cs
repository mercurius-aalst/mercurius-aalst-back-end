using Mercurius.LAN.API.Endpoints;
using Mercurius.LAN.API.Services.GameServices;
using Mercurius.LAN.API.Services.TeamServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Mercurius.LAN.API.Tests;

public class PublicParticipantRouteTests
{
    [Theory]
    [InlineData("v{version:apiVersion}/lan/games/")]
    [InlineData("v{version:apiVersion}/lan/games/{id:guid}")]
    [InlineData("v{version:apiVersion}/lan/teams/")]
    [InlineData("v{version:apiVersion}/lan/teams/{id:guid}")]
    public void PublicReadRoutes_AllowAnonymousAccess(string routePattern)
    {
        var endpoint = GetRouteEndpoint(routePattern);

        Assert.Contains(endpoint.Metadata, metadata => metadata is IAllowAnonymous);
    }

    [Theory]
    [InlineData("v{version:apiVersion}/lan/games/admin")]
    [InlineData("v{version:apiVersion}/lan/games/admin/{id:guid}")]
    [InlineData("v{version:apiVersion}/lan/teams/admin")]
    [InlineData("v{version:apiVersion}/lan/teams/admin/{id:guid}")]
    public void AdminReadRoutes_RequireAdminAuthorization(string routePattern)
    {
        var endpoint = GetRouteEndpoint(routePattern);
        var authorizeMetadata = endpoint.Metadata.OfType<AuthorizeAttribute>().ToList();

        Assert.DoesNotContain(endpoint.Metadata, metadata => metadata is IAllowAnonymous);
        Assert.Contains(authorizeMetadata, metadata => metadata.Roles == "admin");
    }

    private static RouteEndpoint GetRouteEndpoint(string routePattern)
    {
        return GetRouteEndpoints()
            .Single(endpoint => endpoint.RoutePattern.RawText == routePattern);
    }

    private static IEnumerable<RouteEndpoint> GetRouteEndpoints()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddApiVersioning();
        builder.Services.AddScoped<IGameService>(_ => throw new NotSupportedException());
        builder.Services.AddScoped<ITeamService>(_ => throw new NotSupportedException());

        var app = builder.Build();
        app.MapGameEndpoints();
        app.MapTeamEndpoints();

        return ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.Metadata
                .OfType<IHttpMethodMetadata>()
                .Any(metadata => metadata.HttpMethods.Contains("GET")));
    }
}
