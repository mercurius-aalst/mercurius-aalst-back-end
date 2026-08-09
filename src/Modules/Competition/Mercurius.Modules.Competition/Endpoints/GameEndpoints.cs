using Asp.Versioning;
using Mercurius.Modules.Competition.Application.DTOs.Games;
using Mercurius.Modules.Competition.Application.Services;
using Mercurius.Modules.Competition.Contracts;
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

        group.MapGet("/", async (GameService gameService, CancellationToken cancellationToken) =>
        {
            return await gameService.GetAllGamesAsync(cancellationToken);
        })
        .AllowAnonymous();

        group.MapGet("/{id}", async (Guid id, GameService gameService, CancellationToken cancellationToken) =>
        {
            return await gameService.GetGameByIdAsync(id, cancellationToken);
        })
        .AllowAnonymous();

        group.MapPost("/", async ([FromForm] CreateGameDTO createGameDTO, GameService gameService, CancellationToken cancellationToken) =>
        {
            return await gameService.CreateGameAsync(createGameDTO, cancellationToken);
        }).DisableAntiforgery();

        group.MapPatch("/{id}", async (Guid id, [FromForm] UpdateGameDTO updateGameDTO, GameService gameService, CancellationToken cancellationToken) =>
        {
            return await gameService.UpdateGameAsync(id, updateGameDTO, cancellationToken);
        }).DisableAntiforgery();

        group.MapDelete("/{id}", async (Guid id, GameService gameService, CancellationToken cancellationToken) =>
        {
            await gameService.DeleteGameAsync(id, cancellationToken);
        });

        group.MapPut("/{id}/sponsors", async (Guid id, ReplaceGameSponsorsDTO sponsorDTO, GameService gameService, CancellationToken cancellationToken) =>
        {
            return await gameService.ReplaceSponsorPlacementsAsync(id, sponsorDTO, cancellationToken);
        });

        group.MapPut("/{id}/lifecycle-state", async Task<IResult> (Guid id, UpdateGameLifecycleStateRequestDTO request, GameService gameService, CancellationToken cancellationToken) =>
        {
            if (request.State is not { } state || !Enum.IsDefined(state))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["state"] = ["A supported game lifecycle state is required."] });

            switch (state)
            {
                case GameStatus.Scheduled:
                    await gameService.ResetGameAsync(id, cancellationToken);
                    return Results.Ok();
                case GameStatus.InProgress:
                    await gameService.StartGameAsync(id, cancellationToken);
                    return Results.Ok();
                case GameStatus.Completed:
                    return Results.Ok(await gameService.CompleteGameAsync(id, cancellationToken));
                case GameStatus.Canceled:
                    await gameService.CancelGameAsync(id, cancellationToken);
                    return Results.Ok();
                default:
                    return Results.ValidationProblem(new Dictionary<string, string[]> { ["state"] = ["A supported game lifecycle state is required."] });
            }
        });

        return group;
    }
}
