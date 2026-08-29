using Asp.Versioning;
using Mercurius.Modules.Identity.Contracts;
using Mercurius.Modules.Teams.Contracts;
using Mercurius.Modules.Tournament.Application.DTOs.PublicProfiles;
using Mercurius.Modules.Tournament.Contracts;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Mercurius.Modules.Tournament.Endpoints;

internal static class PublicProfileMatchSummaryEndpoints
{
    internal static RouteGroupBuilder MapPublicProfileMatchSummaryEndpoints(this IEndpointRouteBuilder app)
    {
        var apiVersionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("v{version:apiVersion}/lan/public")
            .WithApiVersionSet(apiVersionSet)
            .MapToApiVersion(new ApiVersion(1, 0));

        group.MapGet("/users/{username}/match-summaries", async Task<Results<Ok<PublicProfileMatchSummariesResponseDTO>, NotFound>> (
            string username,
            [FromServices] IIdentityModule identityModule,
            [FromServices] ITournamentModule tournamentModule,
            CancellationToken cancellationToken) =>
        {
            var profile = await identityModule.GetPublicProfileByUsernameAsync(username, cancellationToken);
            if (profile is null)
                return TypedResults.NotFound();

            var summaries = await tournamentModule.GetPublicUserMatchSummariesAsync(profile.Id, cancellationToken);
            return TypedResults.Ok(ToResponse(summaries));
        })
        .AllowAnonymous()
        .WithTags("Users")
        .Produces<PublicProfileMatchSummariesResponseDTO>()
        .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/teams/{teamName}/match-summaries", async Task<Results<Ok<PublicProfileMatchSummariesResponseDTO>, NotFound>> (
            string teamName,
            [FromServices] ITeamsModule teamsModule,
            [FromServices] ITournamentModule tournamentModule,
            CancellationToken cancellationToken) =>
        {
            var teamId = await teamsModule.GetPublicTeamIdByNameAsync(teamName, cancellationToken);
            if (teamId is null)
                return TypedResults.NotFound();

            var summaries = await tournamentModule.GetPublicTeamMatchSummariesAsync(teamId.Value, cancellationToken);
            return TypedResults.Ok(ToResponse(summaries));
        })
        .AllowAnonymous()
        .WithTags("Public Teams")
        .Produces<PublicProfileMatchSummariesResponseDTO>()
        .Produces(StatusCodes.Status404NotFound);

        return group;
    }

    private static PublicProfileMatchSummariesResponseDTO ToResponse(PublicProfileMatchSummarySet summaries) =>
        new()
        {
            PreviousMatches = summaries.PreviousMatches.Select(ToResponse).ToArray(),
            UpcomingMatches = summaries.UpcomingMatches.Select(ToResponse).ToArray()
        };

    private static PublicProfileMatchSummaryResponseDTO ToResponse(PublicProfileMatchSummary summary) =>
        new()
        {
            MatchId = summary.MatchId.Value,
            TournamentId = summary.TournamentId.Value,
            TournamentName = summary.TournamentName,
            OpponentDisplayName = summary.OpponentDisplayName,
            OpponentIsTbd = summary.OpponentIsTbd,
            EstimatedStartTime = summary.EstimatedStartTime,
            EstimatedEndTime = summary.EstimatedEndTime,
            ScheduledStartTime = summary.ScheduledStartTime,
            StartedAtUtc = summary.StartedAtUtc,
            CompletedAtUtc = summary.CompletedAtUtc,
            LifecycleState = summary.LifecycleState,
            ResultKind = summary.ResultKind,
            ParticipantScore = summary.ParticipantScore,
            OpponentScore = summary.OpponentScore,
            RoundNumber = summary.RoundNumber,
            MatchNumber = summary.MatchNumber,
            IsLowerBracketMatch = summary.IsLowerBracketMatch
        };
}
