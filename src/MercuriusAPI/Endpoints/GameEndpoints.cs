using Mercurius.LAN.API.DTOs.GameDTOs;
using Mercurius.LAN.API.DTOs.PlacementDTOs;
using Mercurius.LAN.API.Services.GameServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mercurius.LAN.API.Endpoints;

public static class GameEndpoints
{
    public static RouteGroupBuilder MapGameEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("lan/games")
            .WithTags("Games")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "admin" });

        group.MapGet("/", (IGameService gameService) =>
        {
            return gameService.GetAllGames();
        })
        .AllowAnonymous();

        group.MapGet("/{id}", async (int id, IGameService gameService) =>
        {
            return new GetGameDTO(await gameService.GetGameByIdAsync(id));
        })
        .AllowAnonymous();

        group.MapPost("/", async ([FromForm] CreateGameDTO createGameDTO, IGameService gameService) =>
        {
            return await gameService.CreateGameAsync(createGameDTO);
        });

        group.MapPatch("/{id}", async (int id, [FromForm] UpdateGameDTO updateGameDTO, IGameService gameService) =>
        {
            return await gameService.UpdateGameAsync(id, updateGameDTO);
        });

        group.MapDelete("/{id}", async (int id, IGameService gameService) =>
        {
            await gameService.DeleteGameAsync(id);
        });

        group.MapPost("/{id}/players", async (int id, RegisterGamePlayerDTO registrationDTO, IGameService gameService) =>
        {
            return await gameService.RegisterPlayerAsync(id, registrationDTO.PlayerId);
        });

        group.MapDelete("/{id}/players/{playerId}", async (int id, int playerId, IGameService gameService) =>
        {
            return await gameService.UnregisterPlayerAsync(id, playerId);
        });

        group.MapPost("/{id}/teams", async (int id, RegisterGameTeamDTO registrationDTO, IGameService gameService) =>
        {
            return await gameService.RegisterTeamAsync(id, registrationDTO.TeamId);
        });

        group.MapDelete("/{id}/teams/{teamId}", async (int id, int teamId, IGameService gameService) =>
        {
            return await gameService.UnregisterTeamAsync(id, teamId);
        });

        group.MapPost("/{id}/start", async (int id, IGameService gameService) =>
        {
            await gameService.StartGameAsync(id);
        });

        group.MapPost("/{id}/reset", async (int id, IGameService gameService) =>
        {
            await gameService.ResetGameAsync(id);
        });

        group.MapPost("/{id}/complete", async (int id, IGameService gameService) =>
        {
            return await gameService.CompleteGameAsync(id);
        });

        group.MapPost("/{id}/cancel", async (int id, IGameService gameService) =>
        {
            await gameService.CancelGameAsync(id);
        });

        return group;
    }
}
