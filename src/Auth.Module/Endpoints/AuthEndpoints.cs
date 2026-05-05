using Auth.Module.Services;
using Auth.Module.Models;
using Auth.Module.Services.External;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Auth.Module.Endpoints;

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

        group.MapPost("/external/google/start", async (IExternalAuthService externalAuthService) =>
        {
            return await externalAuthService.StartGoogleAuthAsync();
        })
        .AllowAnonymous();

        group.MapPost("/external/google/callback", async (GoogleAuthCallbackRequest request, IExternalAuthService externalAuthService) =>
        {
            return await externalAuthService.CompleteGoogleAuthAsync(request);
        })
        .AllowAnonymous();

        group.MapPost("/external/google/link/start", async (IExternalAuthService externalAuthService) =>
        {
            return await externalAuthService.StartGoogleLinkAsync();
        });

        group.MapPost("/external/google/link/callback", async (GoogleAuthCallbackRequest request, ClaimsPrincipal user, IExternalAuthService externalAuthService) =>
        {
            await externalAuthService.CompleteGoogleLinkAsync(GetCurrentUserId(user), request);
        });

        group.MapDelete("/external/{provider}/unlink", async (string provider, ClaimsPrincipal user, IExternalAuthService externalAuthService) =>
        {
            await externalAuthService.UnlinkExternalIdentityAsync(GetCurrentUserId(user), provider);
        });

        return group;
    }

    private static Guid GetCurrentUserId(ClaimsPrincipal user)
    {
        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
            throw new UnauthorizedAccessException("Authenticated user identifier is missing.");

        return userId;
    }
}
