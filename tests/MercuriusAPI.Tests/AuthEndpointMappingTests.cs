using Mercurius.LAN.API.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Mercurius.Shared.Services.Auth;
using Auth.Module.Services;
using Mercurius.Shared.DTOs.Auth;
using Auth.Module.Endpoints;

namespace Mercurius.LAN.API.Tests;

public class AuthEndpointMappingTests
{
    [Fact]
    public void MapAuthEndpoints_MapsExpectedRoutes()
    {
        var app = CreateApp();

        app.MapAuthEndpoints();

        var routes = GetRouteEndpoints(app).Select(e => e.RoutePattern.RawText).ToHashSet();

        Assert.Contains("api/v{version:apiVersion}/auth/register", routes);
        Assert.Contains("api/v{version:apiVersion}/auth/login", routes);
        Assert.Contains("api/v{version:apiVersion}/auth/refresh", routes);
        Assert.Contains("api/v{version:apiVersion}/auth/revoke", routes);
    }

    [Fact]
    public void MapAuthEndpoints_OnlyLoginAndRefreshAreAnonymous()
    {
        var app = CreateApp();
        app.MapAuthEndpoints();

        var endpoints = GetRouteEndpoints(app).ToDictionary(e => e.RoutePattern.RawText!);

        Assert.True(HasAllowAnonymous(endpoints["api/v{version:apiVersion}/auth/login"]));
        Assert.True(HasAllowAnonymous(endpoints["api/v{version:apiVersion}/auth/refresh"]));
        Assert.False(HasAllowAnonymous(endpoints["api/v{version:apiVersion}/auth/register"]));
        Assert.False(HasAllowAnonymous(endpoints["api/v{version:apiVersion}/auth/revoke"]));
    }

    [Fact]
    public void MapAuthEndpoints_RegisterRequiresAdminRole()
    {
        var app = CreateApp();
        app.MapAuthEndpoints();

        var registerEndpoint = GetRouteEndpoints(app)
            .Single(e => e.RoutePattern.RawText == "api/v{version:apiVersion}/auth/register");

        var authorizeData = registerEndpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();

        Assert.Contains(authorizeData, a => a.Roles == "admin");
    }

    private static WebApplication CreateApp()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddAuthorization();
        builder.Services.AddSingleton<IAuthService, FakeAuthService>();
        return builder.Build();
    }

    private static IEnumerable<RouteEndpoint> GetRouteEndpoints(WebApplication app)
    {
        var dataSources = ((IEndpointRouteBuilder)app).DataSources;
        return dataSources.SelectMany(ds => ds.Endpoints).OfType<RouteEndpoint>();
    }

    private static bool HasAllowAnonymous(RouteEndpoint endpoint) =>
        endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null;
}


file sealed class FakeAuthService : IAuthService
{
    public Task RegisterAsync(LoginRequest request) => Task.CompletedTask;
    public Task<AuthTokenResponse> LoginAsync(LoginRequest request) => Task.FromResult(new AuthTokenResponse());
    public Task<AuthTokenResponse> RefreshTokenAsync(RefreshTokenRequest request) => Task.FromResult(new AuthTokenResponse());
    public Task RevokeRefreshTokenAsync(RevokeTokenRequest request) => Task.CompletedTask;
}
