using Mercurius.LAN.API.DTOs.PlayerDTOs;
using Mercurius.LAN.API.Services.PlayerServices;
using Microsoft.AspNetCore.Authorization;

namespace Mercurius.LAN.API.Endpoints;

public static class PlayerEndpoints
{
    public static RouteGroupBuilder MapPlayerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("lan/players")
            .WithTags("Players")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "admin" });

        group.MapGet("/", (IPlayerService playerService) =>
        {
            return playerService.GetAllPlayers();
        });

        group.MapGet("/{id}", async (int id, IPlayerService playerService) =>
        {
            return new GetPlayerDTO(await playerService.GetPlayerByIdAsync(id));
        });

        group.MapPost("/", async (CreatePlayerDTO createPlayerDTO, IPlayerService playerService) =>
        {
            return await playerService.CreatePlayerAsync(createPlayerDTO);
        });

        group.MapPatch("/{id}", async (int id, UpdatePlayerDTO updatePlayerDTO, IPlayerService playerService) =>
        {
            return await playerService.UpdatePlayerAsync(id, updatePlayerDTO);
        });

        group.MapDelete("/{id}", async (int id, IPlayerService playerService) =>
        {
            await playerService.DeletePlayerAsync(id);
        });

        return group;
    }
}
