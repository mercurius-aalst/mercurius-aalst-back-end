using Asp.Versioning;
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
        var apiVersionSet = app.NewApiVersionSet()
                .HasApiVersion(new ApiVersion(1, 0))
                .ReportApiVersions()
                .Build();

        var group = app.MapGroup("v{version:apiVersion}/lan/games")
            .WithApiVersionSet(apiVersionSet)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithTags("Games")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "admin" });

        group.MapGet("/", (IGameService gameService) =>
        {
            return gameService.GetAllGames();
        })
        .AllowAnonymous();

        group.MapGet("/{id}", async (Guid id, IGameService gameService) =>
        {
            return new GetGameDTO(await gameService.GetGameByIdAsync(id));
        })
        .AllowAnonymous();

        group.MapPost("/", async ([FromForm] CreateGameDTO createGameDTO, IGameService gameService) =>
        {
            return await gameService.CreateGameAsync(createGameDTO);
        }).DisableAntiforgery();

        group.MapPatch("/{id}", async (Guid id, [FromForm] UpdateGameDTO updateGameDTO, IGameService gameService) =>
        {
            return await gameService.UpdateGameAsync(id, updateGameDTO);
        }).DisableAntiforgery();

        group.MapDelete("/{id}", async (Guid id, IGameService gameService) =>
        {
            await gameService.DeleteGameAsync(id);
        });

        group.MapPut("/{id}/sponsors", async (Guid id, ReplaceGameSponsorsDTO sponsorDTO, IGameService gameService) =>
        {
            return await gameService.ReplaceSponsorPlacementsAsync(id, sponsorDTO);
        });

        group.MapPost("/{id}/start", async (Guid id, IGameService gameService) =>
        {
            await gameService.StartGameAsync(id);
        });

        group.MapPost("/{id}/reset", async (Guid id, IGameService gameService) =>
        {
            await gameService.ResetGameAsync(id);
        });

        group.MapPost("/{id}/complete", async (Guid id, IGameService gameService) =>
        {
            return await gameService.CompleteGameAsync(id);
        });

        group.MapPost("/{id}/cancel", async (Guid id, IGameService gameService) =>
        {
            await gameService.CancelGameAsync(id);
        });

        return group;
    }
}
