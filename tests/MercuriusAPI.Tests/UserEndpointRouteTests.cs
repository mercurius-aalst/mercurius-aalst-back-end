using Mercurius.LAN.API.Endpoints;
using Mercurius.LAN.API.Services.UserServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Mercurius.LAN.API.Tests;

public class UserEndpointRouteTests
{
    [Fact]
    public void UsernameDeleteRoute_RequiresAdminAuthorization()
    {
        var endpoint = GetUserRouteEndpoint("DELETE", "v{version:apiVersion}/lan/users/{username}");

        var authorizeMetadata = endpoint.Metadata.OfType<AuthorizeAttribute>().ToList();

        Assert.DoesNotContain(endpoint.Metadata, metadata => metadata is IAllowAnonymous);
        Assert.Contains(authorizeMetadata, metadata => metadata.Roles == "admin");
    }

    [Fact]
    public void UsernameDeleteCompatibilityRoute_RemainsAvailable()
    {
        var endpoint = GetUserRouteEndpoint("DELETE", "v{version:apiVersion}/lan/users/{username}/account");

        Assert.NotNull(endpoint);
    }

    [Fact]
    public void GuidAndUsernameDeleteRoutes_HaveDistinctRoutePatterns()
    {
        var endpoints = GetUserRouteEndpoints("DELETE").ToList();

        Assert.Contains(endpoints, endpoint => endpoint.RoutePattern.RawText == "v{version:apiVersion}/lan/users/{id:guid}");
        Assert.Contains(endpoints, endpoint => endpoint.RoutePattern.RawText == "v{version:apiVersion}/lan/users/{username}");
    }

    private static RouteEndpoint GetUserRouteEndpoint(string method, string routePattern)
    {
        return GetUserRouteEndpoints(method)
            .Single(endpoint => endpoint.RoutePattern.RawText == routePattern);
    }

    private static IEnumerable<RouteEndpoint> GetUserRouteEndpoints(string method)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddApiVersioning();
        builder.Services.AddScoped<IUserService>(_ => throw new NotSupportedException());

        var app = builder.Build();
        app.MapUserEndpoints();

        return ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.Metadata
                .OfType<IHttpMethodMetadata>()
                .Any(metadata => metadata.HttpMethods.Contains(method)));
    }
}
