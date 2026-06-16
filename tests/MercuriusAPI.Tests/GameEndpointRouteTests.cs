using Mercurius.LAN.API.Endpoints;
using Mercurius.LAN.API.Services.GameServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Mercurius.LAN.API.Tests;

public class GameEndpointRouteTests
{
    [Fact]
    public void GameLifecycleActionRoute_RequiresAdminAuthorization()
    {
        var endpoint = GetGameRouteEndpoint("POST", "v{version:apiVersion}/lan/games/{id}");
        var authorizeMetadata = endpoint.Metadata.OfType<AuthorizeAttribute>().ToList();

        Assert.DoesNotContain(endpoint.Metadata, metadata => metadata is IAllowAnonymous);
        Assert.Contains(authorizeMetadata, metadata => metadata.Roles == "admin");
    }

    [Theory]
    [InlineData("v{version:apiVersion}/lan/games/{id}/start")]
    [InlineData("v{version:apiVersion}/lan/games/{id}/reset")]
    [InlineData("v{version:apiVersion}/lan/games/{id}/complete")]
    [InlineData("v{version:apiVersion}/lan/games/{id}/cancel")]
    public void GameLifecycleActionPathRoutes_AreRemoved(string routePattern)
    {
        var endpoints = GetGameRouteEndpoints("POST");

        Assert.DoesNotContain(endpoints, endpoint => endpoint.RoutePattern.RawText == routePattern);
    }

    private static RouteEndpoint GetGameRouteEndpoint(string method, string routePattern)
    {
        return GetGameRouteEndpoints(method)
            .Single(endpoint => endpoint.RoutePattern.RawText == routePattern);
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
