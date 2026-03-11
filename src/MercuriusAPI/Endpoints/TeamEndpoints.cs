using Mercurius.LAN.API.DTOs.TeamDTOs;
using Mercurius.LAN.API.Services.PlayerServices;
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

        group.MapPost("/", async (CreateTeamDTO createTeamDTO, ITeamService teamService, IPlayerService playerService) =>
        {
            var captain = await playerService.GetPlayerByIdAsync(createTeamDTO.CaptainId);
            return await teamService.CreateTeamAsync(createTeamDTO, captain);
        });

        group.MapDelete("/{id}/players/{playerId}", async (int id, int playerId, ITeamService teamService) =>
        {
            return await teamService.RemovePlayerAsync(id, playerId);
        });

        group.MapPut("/{id}", async (int id, UpdateTeamDTO updateTeamDTO, ITeamService teamService) =>
        {
            return await teamService.UpdateTeamAsync(id, updateTeamDTO);
        });

        group.MapDelete("/{id}", async (int id, ITeamService teamService) =>
        {
            await teamService.DeleteTeamAsync(id);
        });

        group.MapPost("/{id}/players/invite/{playerId}", async (int id, int playerId, ITeamService teamService) =>
        {
            return await teamService.InvitePlayerAsync(id, playerId);
        });

        group.MapPut("/{id}/players/invite/{playerId}", async (int id, int playerId, RespondTeamInviteDTO dto, ITeamService teamService) =>
        {
            return await teamService.RespondToInviteAsync(id, playerId, dto.Accept);
        });

        group.MapGet("/players/{playerId}/invites", async (int playerId, ITeamService teamService) =>
        {
            return await teamService.GetPlayerInvitesAsync(playerId);
        });

        return group;
    }
}
