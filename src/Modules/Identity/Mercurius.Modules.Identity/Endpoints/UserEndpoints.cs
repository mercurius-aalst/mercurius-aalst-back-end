using Asp.Versioning;
using Mercurius.Modules.Identity.DTOs;
using Mercurius.Modules.Shared.Search;
using Mercurius.Modules.Identity.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

namespace Mercurius.Modules.Identity.Endpoints;

internal static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var apiVersionSet = endpoints.NewApiVersionSet()
        .HasApiVersion(new ApiVersion(1, 0))
        .ReportApiVersions()
        .Build();

        var group = endpoints.MapGroup("v{version:apiVersion}/lan/users")
                .WithApiVersionSet(apiVersionSet)
                .MapToApiVersion(new ApiVersion(1, 0))
                .WithTags("Users");

        var publicGroup = endpoints.MapGroup("v{version:apiVersion}/lan/public/users")
            .WithApiVersionSet(apiVersionSet)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithTags("Users");

        publicGroup.MapGet("/{username}", async (string username, IUserService userService) =>
        {
            return await userService.GetPublicUserProfileByUsernameAsync(username);
        })
        .AllowAnonymous();

        group.MapGet("/me", async (ClaimsPrincipal user, IUserService userService) =>
        {
            return await userService.GetCurrentUserAsync(GetAuth0UserId(user));
        })
        .RequireAuthorization();

        group.MapPut("/me", async (CompleteUserProfileRequest request, ClaimsPrincipal user, IUserService userService) =>
        {
            return await userService.CreateCurrentUserAsync(GetAuth0UserId(user), request);
        })
        .RequireAuthorization();

        group.MapPatch("/me", async (UpdateUserProfileRequest request, ClaimsPrincipal user, IUserService userService) =>
        {
            return await userService.UpdateCurrentUserAsync(GetAuth0UserId(user), request);
        })
        .RequireAuthorization();

        group.MapGet("/me/username-availability", async (string username, ClaimsPrincipal user, IUserService userService) =>
        {
            return await userService.CheckUsernameAvailabilityAsync(GetAuth0UserId(user), username);
        })
        .RequireAuthorization();

        group.MapGet("/", async (HttpRequest request, string? query, string? cursor, int? pageSize, ClaimsPrincipal user, IUserService userService, CancellationToken cancellationToken) =>
        {
            if (request.Query.ContainsKey("query"))
            {
                SearchRequest.ValidateQueryLength(SearchRequest.NormalizeQuery(query));
                SearchRequest.ValidatePageSize(pageSize);

                var boundedPageSize = SearchRequest.BoundPageSize(pageSize);
                return Results.Ok(await userService.SearchUsersAsync(query, cursor, boundedPageSize, cancellationToken));
            }

            if (!user.IsInRole("admin"))
                return Results.Forbid();

            return Results.Ok(await userService.GetAllUsersAsync());
        })
        .RequireAuthorization()
        .RequireRateLimiting("authenticated-search");

        group.MapPost("/me/email-verification-requests", async (ClaimsPrincipal user, IUserService userService) =>
        {
            return await userService.ResendVerificationEmailAsync(GetAuth0UserId(user));
        })
        .RequireAuthorization();

        group.MapPost("/me/password-reset-requests", async (ClaimsPrincipal user, IUserService userService) =>
        {
            return await userService.SendPasswordResetEmailAsync(GetAuth0UserId(user));
        })
        .RequireAuthorization();

        group.MapDelete("/me", async (ClaimsPrincipal user, IUserService userService) =>
        {
            return await userService.AnonymizeCurrentUserAsync(GetAuth0UserId(user));
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

        adminGroup.MapPatch("/{id:guid}", async (Guid id, UpdateUserProfileRequest request, IUserService userService) =>
        {
            return await userService.UpdateUserAsync(id, request);
        });

        adminGroup.MapDelete("/{id:guid}", async (Guid id, IUserService userService) =>
        {
            await userService.DeleteUserByIdAsync(id);
        });

        adminGroup.MapDelete("/{username:nonguid}", async (string username, IUserService userService) =>
        {
            await userService.DeleteUserAsync(username);
        });

        adminGroup.MapDelete("/{username}/account", async (string username, IUserService userService) =>
        {
            await userService.DeleteUserAsync(username);
        });

        return group;
    }

    private static string GetAuth0UserId(ClaimsPrincipal user)
    {
        var subject = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(subject))
            throw new UnauthorizedAccessException("Authenticated user id is missing.");

        return subject;
    }
}
