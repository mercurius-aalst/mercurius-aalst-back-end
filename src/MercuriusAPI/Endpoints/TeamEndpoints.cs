using Mercurius.LAN.API.DTOs.TeamDTOs;
using Mercurius.LAN.API.Services.TeamServices;
using Microsoft.AspNetCore.Authorization;

namespace Mercurius.LAN.API.Endpoints;

public static class TeamEndpoints
{
    public static RouteGroupBuilder MapTeamEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("lan/teams")
            .WithTags("Teams")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "admin" });

        group.MapGet("/", (ITeamService teamService) =>
        {
            return teamService.GetAllTeams();
        })
        .AllowAnonymous();

        group.MapGet("/{id}", async (int id, ITeamService teamService) =>
        {
            return new GetTeamDTO(await teamService.GetTeamByIdAsync(id));
        })
        .AllowAnonymous();

        group.MapPost("/", async (CreateTeamDTO createTeamDTO, ITeamService teamService) =>
        {
            return await teamService.CreateTeamAsync(createTeamDTO);
        });

        group.MapDelete("/{id}/users/{userId}", async (int id, int userId, ITeamService teamService) =>
        {
            return await teamService.RemoveMemberAsync(id, userId);
        });

        group.MapPut("/{id}", async (int id, UpdateTeamDTO updateTeamDTO, ITeamService teamService) =>
        {
            return await teamService.UpdateTeamAsync(id, updateTeamDTO);
        });

        group.MapDelete("/{id}", async (int id, ITeamService teamService) =>
        {
            await teamService.DeleteTeamAsync(id);
        });

        group.MapPost("/{id}/users/invite/{userId}", async (int id, int userId, ITeamService teamService) =>
        {
            return await teamService.InviteUserAsync(id, userId);
        });

        group.MapPut("/{id}/users/invite/{userId}", async (int id, int userId, RespondTeamInviteDTO dto, ITeamService teamService) =>
        {
            return await teamService.RespondToInviteAsync(id, userId, dto.Accept);
        });

        group.MapGet("/users/{userId}/invites", async (int userId, ITeamService teamService) =>
        {
            return await teamService.GetUserInvitesAsync(userId);
        });

        return group;
    }
}
