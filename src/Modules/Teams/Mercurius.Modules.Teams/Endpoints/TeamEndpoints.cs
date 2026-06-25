using System.Security.Claims;
using Asp.Versioning;
using Mercurius.Modules.Teams.DTOs;
using Mercurius.Modules.Teams.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Mercurius.Modules.Teams.Endpoints;

internal static class TeamEndpoints
{
    public static RouteGroupBuilder MapTeamEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var apiVersionSet = endpoints.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = endpoints.MapGroup("v{version:apiVersion}/lan/teams")
            .WithApiVersionSet(apiVersionSet)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithTags("Teams");

        var publicGroup = endpoints.MapGroup("v{version:apiVersion}/lan/public/teams")
            .WithApiVersionSet(apiVersionSet)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithTags("Public Teams");

        group.MapGet("/", async (ITeamService teamService, CancellationToken cancellationToken) =>
        {
            return await teamService.GetAllTeamsAsync(cancellationToken);
        })
        .AllowAnonymous();

        group.MapGet("/{id:guid}", async (Guid id, ITeamService teamService, CancellationToken cancellationToken) =>
        {
            return await teamService.GetTeamByIdAsync(id, cancellationToken);
        })
        .AllowAnonymous();

        group.MapPost("/", async (CreateTeamRequestDTO request, ClaimsPrincipal user, ITeamService teamService, CancellationToken cancellationToken) =>
        {
            return await teamService.CreateCurrentUserTeamAsync(GetAuth0UserId(user), request, cancellationToken);
        })
        .RequireAuthorization();

        group.MapGet("/me/summary", async (ClaimsPrincipal user, ITeamService teamService, CancellationToken cancellationToken) =>
        {
            return await teamService.GetCurrentUserTeamSummaryAsync(GetAuth0UserId(user), cancellationToken);
        })
        .RequireAuthorization();

        group.MapGet("/me/invites", async (ClaimsPrincipal user, ITeamService teamService, CancellationToken cancellationToken) =>
        {
            return await teamService.GetCurrentUserInvitesAsync(GetAuth0UserId(user), cancellationToken);
        })
        .RequireAuthorization();

        group.MapGet("/me/sent-invites", async (ClaimsPrincipal user, ITeamService teamService, CancellationToken cancellationToken) =>
        {
            return await teamService.GetCurrentUserSentInvitesAsync(GetAuth0UserId(user), cancellationToken);
        })
        .RequireAuthorization();

        group.MapPost("/{id}/leave", async (Guid id, ClaimsPrincipal user, ITeamService teamService, CancellationToken cancellationToken) =>
        {
            return await teamService.LeaveTeamAsync(GetAuth0UserId(user), id, cancellationToken);
        })
        .RequireAuthorization();

        group.MapDelete("/{id:guid}/members/{userId:guid}", async (Guid id, Guid userId, ClaimsPrincipal user, ITeamService teamService, CancellationToken cancellationToken) =>
        {
            return await teamService.RemoveMemberAsync(GetAuth0UserId(user), id, userId, cancellationToken);
        })
        .RequireAuthorization();

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal user, ITeamService teamService, CancellationToken cancellationToken) =>
        {
            await teamService.DeleteTeamAsync(GetAuth0UserId(user), id, cancellationToken);
            return Results.NoContent();
        })
        .RequireAuthorization();

        group.MapPost("/{id}/invites/{userId}", async (Guid id, Guid userId, ClaimsPrincipal user, ITeamService teamService, CancellationToken cancellationToken) =>
        {
            return await teamService.InviteUserAsync(GetAuth0UserId(user), id, userId, cancellationToken);
        })
        .RequireAuthorization();

        group.MapDelete("/{id}/invites/{inviteId}", async (Guid id, Guid inviteId, ClaimsPrincipal user, ITeamService teamService, CancellationToken cancellationToken) =>
        {
            return await teamService.CancelInviteAsync(GetAuth0UserId(user), id, inviteId, cancellationToken);
        })
        .RequireAuthorization();

        group.MapPut("/invites/{inviteId}", async (Guid inviteId, RespondTeamInviteRequestDTO request, ClaimsPrincipal user, ITeamService teamService, CancellationToken cancellationToken) =>
        {
            return await teamService.RespondToInviteAsync(GetAuth0UserId(user), inviteId, request.Accept, cancellationToken);
        })
        .RequireAuthorization();

        group.MapPut("/{id}/captain", async (Guid id, TransferCaptainRequestDTO request, ClaimsPrincipal user, ITeamService teamService, CancellationToken cancellationToken) =>
        {
            return await teamService.TransferCaptainAsync(GetAuth0UserId(user), id, request.NewCaptainUserId, cancellationToken);
        })
        .RequireAuthorization();

        group.MapPost("/{id}/logo", async (Guid id, IFormFile logo, ClaimsPrincipal user, ITeamService teamService, CancellationToken cancellationToken) =>
        {
            return await teamService.UploadTeamLogoAsync(GetAuth0UserId(user), id, logo, cancellationToken);
        })
        .Accepts<IFormFile>("multipart/form-data")
        .DisableAntiforgery()
        .RequireAuthorization();

        group.MapDelete("/{id}/logo", async (Guid id, ClaimsPrincipal user, ITeamService teamService, CancellationToken cancellationToken) =>
        {
            return await teamService.RemoveTeamLogoAsync(GetAuth0UserId(user), id, cancellationToken);
        })
        .RequireAuthorization();

        publicGroup.MapGet("/{teamName}", async (string teamName, ITeamService teamService, CancellationToken cancellationToken) =>
        {
            return await teamService.GetPublicTeamProfileAsync(teamName, cancellationToken);
        })
        .AllowAnonymous();

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
