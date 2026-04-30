using Mercurius.LAN.API.DTOs.Auth;
using Mercurius.LAN.API.Services.UserServices;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Mercurius.LAN.API.Endpoints;

public static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("lan/users")
            .WithTags("Users")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "admin" });

        group.MapPost("/", async (CreateUserProfileRequest request, IUserService userService) =>
        {
            return await userService.CreateUserAsync(request);
        });

        group.MapGet("/{id:int}", async (int id, IUserService userService) =>
        {
            return await userService.GetUserByIdAsync(id);
        });

        group.MapGet("/", async (IUserService userService) =>
        {
            return await userService.GetAllUsersAsync();
        });

        group.MapPatch("/{id:int}", async (int id, UpdateUserProfileRequest request, IUserService userService) =>
        {
            return await userService.UpdateUserAsync(id, request);
        });

        group.MapDelete("/{id:int}", async (int id, IUserService userService) =>
        {
            await userService.DeleteUserByIdAsync(id);
        });

        group.MapDelete("/{username}/account", async (string username, IUserService userService) =>
        {
            await userService.DeleteUserAsync(username);
        });

        group.MapPost("/{username}/roles", async (string username, AddUserRoleRequest request, IUserService userService) =>
        {
            await userService.AddRoleToUserAsync(username, request);
        });

        group.MapDelete("/{username}/roles/{role}", async (string username, string role, IUserService userService) =>
        {
            await userService.DeleteRoleFromUserAsync(username, role);
        });

        group.MapPatch("/{username}/password", async (string username, ChangePasswordRequest request, ClaimsPrincipal user, IUserService userService) =>
        {
            var authenticatedUsername = user.FindFirstValue(ClaimTypes.Name);

            if (authenticatedUsername == null || !authenticatedUsername.Equals(username, StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("You are not authorized to change this user's password.");

            await userService.ChangePasswordAsync(username, request);
        });

        return group;
    }
}
