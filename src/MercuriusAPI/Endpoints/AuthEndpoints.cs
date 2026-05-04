using Mercurius.LAN.API.DTOs.Auth;
using Mercurius.LAN.API.Services.Auth;
using Microsoft.AspNetCore.Authorization;

namespace Mercurius.LAN.API.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("api/v{version:apiVersion}/auth")
            .WithGroupName("v1")
            .WithTags("Auth")
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


        group.MapPost("/external/login", async (ExternalAuthRequest request, IAuthService authService) =>
        {
            return await authService.ExternalLoginAsync(request);
        })
        .AllowAnonymous();

        group.MapPost("/external/link", async (ExternalAuthRequest request, IAuthService authService, System.Security.Claims.ClaimsPrincipal user) =>
        {
            var username = user.Identity?.Name ?? string.Empty;
            await authService.LinkExternalIdentityAsync(username, request);
        });

        return group;
    }
}
