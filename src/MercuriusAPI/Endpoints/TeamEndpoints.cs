using Asp.Versioning;
using Mercurius.LAN.API.DTOs.TeamDTOs;
using Mercurius.LAN.API.Services.TeamServices;
using Microsoft.AspNetCore.Authorization;
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
            .WithTags("Teams")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "admin" });

        group.MapGet("/", (ClaimsPrincipal user, ITeamService teamService) =>
        {
            var teams = teamService.GetAllTeams().ToList();
            if (IsAdmin(user))
                return Results.Ok(teams);

            var includePlatformIds = user.Identity?.IsAuthenticated == true;
            return Results.Ok(teams.Select(team => new GetPublicTeamDTO(team, includePlatformIds)));
        })
        .AllowAnonymous();

        group.MapGet("/{id}", async (Guid id, ClaimsPrincipal user, ITeamService teamService) =>
        {
            var team = await teamService.GetTeamByIdAsync(id);
            if (IsAdmin(user))
                return Results.Ok(new GetTeamDTO(team));

            var includePlatformIds = user.Identity?.IsAuthenticated == true;
            return Results.Ok(new GetPublicTeamDTO(team, includePlatformIds));
        })
        .AllowAnonymous();

        group.MapPost("/", async (CreateTeamDTO createTeamDTO, ITeamService teamService) =>
        {
            return await teamService.CreateTeamAsync(createTeamDTO);
        });

        group.MapDelete("/{id}/users/{userId}", async (Guid id, Guid userId, ITeamService teamService) =>
        {
            return await teamService.RemoveMemberAsync(id, userId);
        });

        group.MapPut("/{id}", async (Guid id, UpdateTeamDTO updateTeamDTO, ITeamService teamService) =>
        {
            return await teamService.UpdateTeamAsync(id, updateTeamDTO);
        });

        group.MapDelete("/{id}", async (Guid id, ITeamService teamService) =>
        {
            await teamService.DeleteTeamAsync(id);
        });

        group.MapPost("/{id}/users/invite/{userId}", async (Guid id, Guid userId, ITeamService teamService) =>
        {
            return await teamService.InviteUserAsync(id, userId);
        });

        group.MapPut("/{id}/users/invite/{userId}", async (Guid id, Guid userId, RespondTeamInviteDTO dto, ITeamService teamService) =>
        {
            return await teamService.RespondToInviteAsync(id, userId, dto.Accept);
        });

        group.MapGet("/users/{userId}/invites", async (Guid userId, ITeamService teamService) =>
        {
            return await teamService.GetUserInvitesAsync(userId);
        });

        return group;
    }

    private static bool IsAdmin(ClaimsPrincipal user)
    {
        return user.Identity?.IsAuthenticated == true && user.IsInRole("admin");
    }
}
