using Asp.Versioning;
using Mercurius.LAN.API.DTOs.TeamDTOs;
using Mercurius.LAN.API.Services.TeamServices;
using System.Security.Claims;

namespace Mercurius.LAN.API.Endpoints;

public static class TeamEndpoints
{
    public static RouteGroupBuilder MapTeamEndpoints(this WebApplication app)
    {
        var apiVersionSet = app.NewApiVersionSet()
        .HasApiVersion(new ApiVersion(1, 0))
        .ReportApiVersions()
        .Build();

        var group = app.MapGroup("v{version:apiVersion}/lan/teams")
            .WithApiVersionSet(apiVersionSet)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithTags("Teams");

        var publicGroup = app.MapGroup("v{version:apiVersion}/lan/public/teams")
            .WithApiVersionSet(apiVersionSet)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithTags("Public Teams");

        group.MapGet("/", (ITeamService teamService) =>
        {
            return teamService.GetAllTeams();
        })
        .AllowAnonymous();

        group.MapGet("/{id:guid}", async (Guid id, ITeamService teamService) =>
        {
            return new GetTeamDTO(await teamService.GetTeamByIdAsync(id));
        })
        .AllowAnonymous();

        group.MapPost("/", async (CreateTeamDTO createTeamDTO, ClaimsPrincipal user, ITeamService teamService) =>
        {
            return await teamService.CreateCurrentUserTeamAsync(GetAuth0UserId(user), createTeamDTO);
        })
        .RequireAuthorization();

        group.MapGet("/me/summary", async (ClaimsPrincipal user, ITeamService teamService) =>
        {
            return await teamService.GetCurrentUserTeamSummaryAsync(GetAuth0UserId(user));
        })
        .RequireAuthorization();

        group.MapGet("/me/invites", async (ClaimsPrincipal user, ITeamService teamService) =>
        {
            return await teamService.GetCurrentUserInvitesAsync(GetAuth0UserId(user));
        })
        .RequireAuthorization();

        group.MapGet("/me/sent-invites", async (ClaimsPrincipal user, ITeamService teamService) =>
        {
            return await teamService.GetCurrentUserSentInvitesAsync(GetAuth0UserId(user));
        })
        .RequireAuthorization();

        group.MapPost("/{id}/leave", async (Guid id, ClaimsPrincipal user, ITeamService teamService) =>
        {
            return await teamService.LeaveTeamAsync(GetAuth0UserId(user), id);
        })
        .RequireAuthorization();

        group.MapPost("/{id}/invites/{userId}", async (Guid id, Guid userId, ClaimsPrincipal user, ITeamService teamService) =>
        {
            return await teamService.InviteUserAsync(GetAuth0UserId(user), id, userId);
        })
        .RequireAuthorization();

        group.MapDelete("/{id}/invites/{inviteId}", async (Guid id, Guid inviteId, ClaimsPrincipal user, ITeamService teamService) =>
        {
            return await teamService.CancelInviteAsync(GetAuth0UserId(user), id, inviteId);
        })
        .RequireAuthorization();

        group.MapPut("/invites/{inviteId}", async (Guid inviteId, RespondTeamInviteDTO dto, ClaimsPrincipal user, ITeamService teamService) =>
        {
            return await teamService.RespondToInviteAsync(GetAuth0UserId(user), inviteId, dto.Accept);
        })
        .RequireAuthorization();

        group.MapPut("/{id}/captain", async (Guid id, TransferCaptainDTO dto, ClaimsPrincipal user, ITeamService teamService) =>
        {
            return await teamService.TransferCaptainAsync(GetAuth0UserId(user), id, dto.NewCaptainUserId);
        })
        .RequireAuthorization();

        group.MapPost("/{id}/logo", async (Guid id, IFormFile logo, ClaimsPrincipal user, ITeamService teamService) =>
        {
            return await teamService.UploadTeamLogoAsync(GetAuth0UserId(user), id, logo);
        })
        .Accepts<IFormFile>("multipart/form-data")
        .DisableAntiforgery()
        .RequireAuthorization();

        group.MapDelete("/{id}/logo", async (Guid id, ClaimsPrincipal user, ITeamService teamService) =>
        {
            return await teamService.RemoveTeamLogoAsync(GetAuth0UserId(user), id);
        })
        .RequireAuthorization();

        publicGroup.MapGet("/{teamName}", async (string teamName, ITeamService teamService) =>
        {
            return await teamService.GetPublicTeamProfileAsync(teamName);
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
