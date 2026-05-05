using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Mercurius.LAN.API.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Mercurius.Shared.Services.Auth;
using Auth.Module.Services;

namespace Mercurius.LAN.API.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("api/v{version:apiVersion}/auth")
            .WithGroupName("v1")
            .RequireAuthorization();

        group.MapPost("/register", async (LoginRequest request, IAuthService authService) =>
        {
            await authService.RegisterAsync(request);
        })
        .RequireAuthorization(new AuthorizeAttribute { Roles = "admin" });

        group.MapPost("/login", async (LoginRequest request, IAuthService authService) =>
        {
            return await authService.LoginAsync(request);
        })
        .AllowAnonymous();

        group.MapPost("/refresh", async (RefreshTokenRequest request, IAuthService authService) =>
        {
            return await authService.RefreshTokenAsync(request);
        })
        .AllowAnonymous();

        group.MapPost("/revoke", async (RevokeTokenRequest request, IAuthService authService) =>
        {
            await authService.RevokeRefreshTokenAsync(request);
        });

        return group;
    }
}
