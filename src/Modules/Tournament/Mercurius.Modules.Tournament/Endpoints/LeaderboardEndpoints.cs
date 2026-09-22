using Asp.Versioning;
using Mercurius.Modules.Tournament.Application.DTOs.Leaderboards;
using Mercurius.Modules.Tournament.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Mercurius.Modules.Tournament.Endpoints;

internal static class LeaderboardEndpoints
{
    internal static RouteGroupBuilder MapLeaderboardEndpoints(this IEndpointRouteBuilder app)
    {
        var versions = app.NewApiVersionSet().HasApiVersion(new ApiVersion(1, 0)).ReportApiVersions().Build();
        var group = app.MapGroup("v{version:apiVersion}/lan/tournaments/{tournamentId:guid}/leaderboard")
            .WithApiVersionSet(versions)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithTags("Tournament leaderboard")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "admin" });

        group.MapGet("/", (Guid tournamentId, [FromServices] ILeaderboardService service, CancellationToken cancellationToken) =>
            service.GetPublicLeaderboardAsync(tournamentId, cancellationToken))
            .AllowAnonymous()
            .Produces<LeaderboardResponseDTO>();

        group.MapGet("/admin", (Guid tournamentId, [FromServices] ILeaderboardService service, CancellationToken cancellationToken) =>
            service.GetAdminLeaderboardAsync(tournamentId, cancellationToken))
            .Produces<AdminLeaderboardResponseDTO>();

        group.MapPost("/attempts", (Guid tournamentId, RecordLeaderboardAttemptDTO request, [FromServices] ILeaderboardService service, CancellationToken cancellationToken) =>
            service.RecordAttemptAsync(tournamentId, request, cancellationToken))
            .Produces<AdminLeaderboardParticipantDTO>();

        group.MapPut("/attempts/{attemptId:guid}", (Guid tournamentId, Guid attemptId, UpdateLeaderboardAttemptDTO request, [FromServices] ILeaderboardService service, CancellationToken cancellationToken) =>
            service.UpdateAttemptAsync(tournamentId, attemptId, request, cancellationToken))
            .Produces<LeaderboardAttemptDTO>();

        group.MapDelete("/attempts/{attemptId:guid}", async (Guid tournamentId, Guid attemptId, Guid rowVersion, [FromServices] ILeaderboardService service, CancellationToken cancellationToken) =>
        {
            await service.RemoveAttemptAsync(tournamentId, attemptId, rowVersion, cancellationToken);
            return Results.NoContent();
        }).Produces(StatusCodes.Status204NoContent);

        return group;
    }
}
