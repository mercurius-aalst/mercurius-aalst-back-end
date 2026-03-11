using Mercurius.LAN.API.DTOs.GameDTOs;
using Mercurius.LAN.API.DTOs.PlacementDTOs;
using Mercurius.LAN.API.Services.GameServices;
using Mercurius.LAN.API.Services.ParticipantServices;
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

        group.MapPost("/{id}/participants/{participantId}", async (int id, int participantId, IGameService gameService, IParticipantService participantService) =>
        {
            var participant = await participantService.GetParticipantByIdAsync(participantId);
            return await gameService.AddParticipantAsync(id, participant);
        });

        group.MapDelete("/{id}/participants/{participantId}", async (int id, int participantId, IGameService gameService, IParticipantService participantService) =>
        {
            var participant = await participantService.GetParticipantByIdAsync(participantId);
            return await gameService.RemoveParticipantAsync(id, participant);
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
