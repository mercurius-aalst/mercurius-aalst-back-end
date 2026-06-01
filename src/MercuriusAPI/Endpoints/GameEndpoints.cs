using Asp.Versioning;
using Mercurius.LAN.API.DTOs.GameDTOs;
using Mercurius.LAN.API.DTOs.Public;
using Mercurius.LAN.API.Services.GameServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

        group.MapGet("/", (ClaimsPrincipal user, IGameService gameService) =>
        {
            return Results.Ok(gameService.GetAllPublicGames(GetPublicAudience(user)).ToList());
        })
        .AllowAnonymous();

        group.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user, IGameService gameService) =>
        {
            return Results.Ok(await gameService.GetPublicGameByIdAsync(id, GetPublicAudience(user)));
        })
        .AllowAnonymous();

        group.MapGet("/admin", (IGameService gameService) =>
        {
            return Results.Ok(gameService.GetAllGames().ToList());
        });

        group.MapGet("/admin/{id:guid}", async (Guid id, IGameService gameService) =>
        {
            return Results.Ok(new GetGameDTO(await gameService.GetGameByIdAsync(id)));
        });

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

        group.MapPost("/{id}/users", async (Guid id, RegisterGameUserDTO registrationDTO, IGameService gameService) =>
        {
            return await gameService.RegisterUserAsync(id, registrationDTO.UserId);
        });

        group.MapDelete("/{id}/users/{userId}", async (Guid id, Guid userId, IGameService gameService) =>
        {
            return await gameService.UnregisterUserAsync(id, userId);
        });

        group.MapPost("/{id}/teams", async (Guid id, RegisterGameTeamDTO registrationDTO, IGameService gameService) =>
        {
            return await gameService.RegisterTeamAsync(id, registrationDTO.TeamId);
        });

        group.MapDelete("/{id}/teams/{teamId}", async (Guid id, Guid teamId, IGameService gameService) =>
        {
            return await gameService.UnregisterTeamAsync(id, teamId);
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

    private static PublicAudience GetPublicAudience(ClaimsPrincipal user)
    {
        return user.Identity?.IsAuthenticated == true
            ? PublicAudience.Authenticated
            : PublicAudience.Anonymous;
    }
}
