using Asp.Versioning;
using Mercurius.Modules.Competition.Application.DTOs.Games;
using Mercurius.Modules.Competition.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mercurius.Modules.Competition.Endpoints;

internal static class GameEndpoints
{
    internal static RouteGroupBuilder MapGameEndpoints(this IEndpointRouteBuilder app)
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

        group.MapGet("/", async (IGameService gameService, CancellationToken cancellationToken) =>
        {
            return await gameService.GetAllGamesAsync(cancellationToken);
        })
        .AllowAnonymous();

        group.MapGet("/{id}", async (Guid id, IGameService gameService, CancellationToken cancellationToken) =>
        {
            return await gameService.GetGameByIdAsync(id, cancellationToken);
        })
        .AllowAnonymous();

        group.MapPost("/", async ([FromForm] CreateGameDTO createGameDTO, IGameService gameService, CancellationToken cancellationToken) =>
        {
            return await gameService.CreateGameAsync(createGameDTO, cancellationToken);
        }).DisableAntiforgery();

        group.MapPatch("/{id}", async (Guid id, [FromForm] UpdateGameDTO updateGameDTO, IGameService gameService, CancellationToken cancellationToken) =>
        {
            return await gameService.UpdateGameAsync(id, updateGameDTO, cancellationToken);
        }).DisableAntiforgery();

        group.MapDelete("/{id}", async (Guid id, IGameService gameService, CancellationToken cancellationToken) =>
        {
            await gameService.DeleteGameAsync(id, cancellationToken);
        });

        group.MapPut("/{id}/sponsors", async (Guid id, ReplaceGameSponsorsDTO sponsorDTO, IGameService gameService, CancellationToken cancellationToken) =>
        {
            return await gameService.ReplaceSponsorPlacementsAsync(id, sponsorDTO, cancellationToken);
        });

        group.MapPost("/{id}/start", async (Guid id, IGameService gameService, CancellationToken cancellationToken) =>
        {
            await gameService.StartGameAsync(id, cancellationToken);
        });

        group.MapPost("/{id}/reset", async (Guid id, IGameService gameService, CancellationToken cancellationToken) =>
        {
            await gameService.ResetGameAsync(id, cancellationToken);
        });

        group.MapPost("/{id}/complete", async (Guid id, IGameService gameService, CancellationToken cancellationToken) =>
        {
            return await gameService.CompleteGameAsync(id, cancellationToken);
        });

        group.MapPost("/{id}/cancel", async (Guid id, IGameService gameService, CancellationToken cancellationToken) =>
        {
            await gameService.CancelGameAsync(id, cancellationToken);
        });

        return group;
    }
}
