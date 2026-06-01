using Asp.Versioning;
using Mercurius.LAN.API.Endpoints;
using Mercurius.LAN.API.Services.GameServices;
using Mercurius.LAN.API.Services.MatchServices;
using Mercurius.LAN.API.Services.SponsorServices;
using Mercurius.LAN.API.Services.TeamServices;
using Mercurius.LAN.API.Services.UserServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Mercurius.LAN.API.Tests.Contracts;

public class RouteContractTests
{
    [Theory]
    [InlineData("POST", "v{version:apiVersion}/lan/games/")]
    [InlineData("PATCH", "v{version:apiVersion}/lan/games/{id}")]
    [InlineData("DELETE", "v{version:apiVersion}/lan/games/{id}")]
    [InlineData("POST", "v{version:apiVersion}/lan/teams/")]
    [InlineData("PUT", "v{version:apiVersion}/lan/teams/{id}")]
    [InlineData("DELETE", "v{version:apiVersion}/lan/teams/{id}")]
    [InlineData("POST", "v{version:apiVersion}/lan/sponsors/")]
    [InlineData("PATCH", "v{version:apiVersion}/lan/sponsors/{id}")]
    [InlineData("DELETE", "v{version:apiVersion}/lan/sponsors/{id}")]
    [InlineData("PUT", "v{version:apiVersion}/lan/matches/{id}")]
    [InlineData("POST", "v{version:apiVersion}/lan/users/")]
    [InlineData("PATCH", "v{version:apiVersion}/lan/users/{id:guid}")]
    [InlineData("DELETE", "v{version:apiVersion}/lan/users/{id:guid}")]
    public void AdminRoutes_RequireAdminRole(string method, string routePattern)
    {
        var endpoint = GetRouteEndpoint(method, routePattern);

        AssertAdminOnly(endpoint);
    }

    [Theory]
    [InlineData("GET", "v{version:apiVersion}/lan/games/")]
    [InlineData("GET", "v{version:apiVersion}/lan/games/{id}")]
    [InlineData("GET", "v{version:apiVersion}/lan/teams/")]
    [InlineData("GET", "v{version:apiVersion}/lan/teams/{id}")]
    [InlineData("GET", "v{version:apiVersion}/lan/sponsors/")]
    [InlineData("GET", "v{version:apiVersion}/lan/sponsors/{id}")]
    [InlineData("GET", "v{version:apiVersion}/lan/matches/{id}")]
    public void PublicReadRoutes_AllowAnonymous(string method, string routePattern)
    {
        var endpoint = GetRouteEndpoint(method, routePattern);

        Assert.Contains(endpoint.Metadata, metadata => metadata is IAllowAnonymous);
    }

    [Fact]
    public void AdminUserDeleteRoutes_RemainDistinctForCompatibility()
    {
        var deleteRoutes = GetRouteEndpoints("DELETE")
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("v{version:apiVersion}/lan/users/", StringComparison.Ordinal) == true)
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .ToList();

        Assert.Contains("v{version:apiVersion}/lan/users/{id:guid}", deleteRoutes);
        Assert.Contains("v{version:apiVersion}/lan/users/{username}/account", deleteRoutes);
        Assert.Equal(deleteRoutes.Count, deleteRoutes.Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    [InlineData("GET", "v{version:apiVersion}/lan/public/users/{username}")]
    [InlineData("GET", "v{version:apiVersion}/lan/public/teams/{teamName}")]
    [InlineData("GET", "v{version:apiVersion}/lan/public/search")]
    public void FuturePublicRoutes_AllowAnonymousWhenPresent(string method, string routePattern)
    {
        var endpoint = GetRouteEndpoints(method)
            .SingleOrDefault(endpoint => endpoint.RoutePattern.RawText == routePattern);

        if (endpoint is null)
            return;

        Assert.Contains(endpoint.Metadata, metadata => metadata is IAllowAnonymous);
    }

    private static RouteEndpoint GetRouteEndpoint(string method, string routePattern)
    {
        return GetRouteEndpoints(method)
            .Single(endpoint => endpoint.RoutePattern.RawText == routePattern);
    }

    private static IEnumerable<RouteEndpoint> GetRouteEndpoints(string method)
    {
        return GetAllRouteEndpoints()
            .Where(endpoint => endpoint.Metadata
                .OfType<IHttpMethodMetadata>()
                .Any(metadata => metadata.HttpMethods.Contains(method)));
    }

    private static IReadOnlyList<RouteEndpoint> GetAllRouteEndpoints()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddApiVersioning();
        builder.Services.AddScoped<IGameService>(_ => throw new NotSupportedException());
        builder.Services.AddScoped<ITeamService>(_ => throw new NotSupportedException());
        builder.Services.AddScoped<ISponsorService>(_ => throw new NotSupportedException());
        builder.Services.AddScoped<IMatchService>(_ => throw new NotSupportedException());
        builder.Services.AddScoped<IUserService>(_ => throw new NotSupportedException());

        var app = builder.Build();
        app.MapGameEndpoints();
        app.MapTeamEndpoints();
        app.MapSponsorEndpoints();
        app.MapMatchEndpoints();
        app.MapUserEndpoints();

        return ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .ToList();
    }

    private static void AssertAdminOnly(RouteEndpoint endpoint)
    {
        var authorizeMetadata = endpoint.Metadata.OfType<AuthorizeAttribute>().ToList();

        Assert.DoesNotContain(endpoint.Metadata, metadata => metadata is IAllowAnonymous);
        Assert.Contains(authorizeMetadata, metadata => metadata.Roles == "admin");
    }
}
