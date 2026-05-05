using Asp.Versioning;
using Mercurius.LAN.API.DTOs.Auth;
using Mercurius.LAN.API.Services.UserServices;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Mercurius.LAN.API.Endpoints;

public static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this WebApplication app)
    {
        var apiVersionSet = app.NewApiVersionSet()
        .HasApiVersion(new ApiVersion(1, 0))
        .ReportApiVersions()
        .Build();

        var group = app.MapGroup("api/v{version:apiVersion}/lan/users")
                .WithApiVersionSet(apiVersionSet)
                .MapToApiVersion(new ApiVersion(1, 0))
                .WithTags("Users");

        group.MapGet("/me", async (ClaimsPrincipal user, IUserService userService) =>
        {
            return await userService.GetCurrentUserAsync(GetAuth0Subject(user));
        })
        .RequireAuthorization();

        group.MapPost("/me/complete-profile", async (CompleteUserProfileRequest request, ClaimsPrincipal user, IUserService userService) =>
        {
            return await userService.CompleteProfileAsync(GetAuth0Subject(user), request);
        })
        .RequireAuthorization();

        var adminGroup = group.MapGroup("")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "admin" });

        adminGroup.MapPost("/", async (CreateUserProfileRequest request, IUserService userService) =>
        {
            return await userService.CreateUserAsync(request);
        });

        adminGroup.MapGet("/{id:guid}", async (Guid id, IUserService userService) =>
        {
            return await userService.GetUserByIdAsync(id);
        });

        adminGroup.MapGet("/", async (IUserService userService) =>
        {
            return await userService.GetAllUsersAsync();
        });

        adminGroup.MapPatch("/{id:guid}", async (Guid id, UpdateUserProfileRequest request, IUserService userService) =>
        {
            return await userService.UpdateUserAsync(id, request);
        });

        adminGroup.MapDelete("/{id:guid}", async (Guid id, IUserService userService) =>
        {
            await userService.DeleteUserByIdAsync(id);
        });

        adminGroup.MapDelete("/{username}/account", async (string username, IUserService userService) =>
        {
            await userService.DeleteUserAsync(username);
        });

        return group;
    }

    private static string GetAuth0Subject(ClaimsPrincipal user)
    {
        var subject = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(subject))
            throw new UnauthorizedAccessException("Authenticated user subject is missing.");

        return subject;
    }
}
