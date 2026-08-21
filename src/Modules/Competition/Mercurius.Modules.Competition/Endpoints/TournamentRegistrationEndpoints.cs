using Asp.Versioning;
using Mercurius.Modules.Competition.Application.DTOs.Registrations;
using Mercurius.Modules.Competition.Application.Services;
using Mercurius.Modules.Competition.Contracts;
using Mercurius.Modules.Shared.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Mercurius.Modules.Competition.Endpoints;

internal static class TournamentRegistrationEndpoints
{
    internal static RouteGroupBuilder MapTournamentRegistrationEndpoints(this IEndpointRouteBuilder app)
    {
        var apiVersionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("v{version:apiVersion}/lan/games/{gameId:guid}/registrations")
            .WithApiVersionSet(apiVersionSet)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithTags("Tournament Registrations");

        group.MapGet("/me", async (Guid gameId, ClaimsPrincipal user, ITournamentRegistrationService registrationService, CancellationToken cancellationToken) =>
        {
            return await registrationService.GetCurrentUserStateAsync(GetAuth0UserId(user), gameId, cancellationToken);
        })
        .RequireAuthorization();

        group.MapGet("/individual/eligibility", async (Guid gameId, ClaimsPrincipal user, ITournamentRegistrationService registrationService, CancellationToken cancellationToken) =>
        {
            return await registrationService.CheckIndividualEligibilityAsync(GetAuth0UserId(user), gameId, cancellationToken);
        })
        .RequireAuthorization();

        group.MapGet("/teams/{teamId:guid}/eligibility", async (Guid gameId, Guid teamId, ClaimsPrincipal user, ITournamentRegistrationService registrationService, CancellationToken cancellationToken) =>
        {
            return await registrationService.CheckTeamEligibilityAsync(GetAuth0UserId(user), gameId, teamId, cancellationToken);
        })
        .RequireAuthorization();

        group.MapPost("/teams/{teamId:guid}/roster/eligibility", async Task<IResult> (Guid gameId, Guid teamId, SubmitTeamRosterDTO request, ClaimsPrincipal user, ITournamentRegistrationService registrationService, CancellationToken cancellationToken) =>
        {
            var validationProblem = ValidateRosterUserIds(request.UserIds);
            if (validationProblem is not null)
                return validationProblem;

            return Results.Ok(await registrationService.CheckRosterEligibilityAsync(GetAuth0UserId(user), gameId, teamId, request.UserIds, cancellationToken));
        })
        .RequireAuthorization()
        .Produces<RosterCandidateEligibilityResponseDTO>()
        .ProducesValidationProblem();

        group.MapPut("/individual/me", async (Guid gameId, ClaimsPrincipal user, ITournamentRegistrationService registrationService, CancellationToken cancellationToken) =>
        {
            return await registrationService.RegisterIndividualAsync(GetAuth0UserId(user), gameId, cancellationToken);
        })
        .RequireAuthorization();

        group.MapDelete("/individual/me", async (Guid gameId, ClaimsPrincipal user, ITournamentRegistrationService registrationService, CancellationToken cancellationToken) =>
        {
            await registrationService.UnregisterIndividualAsync(GetAuth0UserId(user), gameId, cancellationToken);
            return Results.NoContent();
        })
        .RequireAuthorization();

        group.MapPut("/teams/{teamId:guid}/roster", async Task<IResult> (Guid gameId, Guid teamId, SubmitTeamRosterDTO request, ClaimsPrincipal user, ITournamentRegistrationService registrationService, CancellationToken cancellationToken) =>
        {
            var validationProblem = ValidateRosterUserIds(request.UserIds);
            if (validationProblem is not null)
                return validationProblem;

            var requestWithRouteTeam = request with { TeamId = teamId };
            return Results.Ok(await registrationService.SubmitTeamRosterAsync(GetAuth0UserId(user), gameId, requestWithRouteTeam, cancellationToken));
        })
        .RequireAuthorization()
        .Produces<TournamentRegistrationDTO>()
        .ProducesValidationProblem();

        group.MapDelete("/teams/{teamId:guid}", async (Guid gameId, Guid teamId, ClaimsPrincipal user, ITournamentRegistrationService registrationService, CancellationToken cancellationToken) =>
        {
            await registrationService.UnregisterTeamAsync(GetAuth0UserId(user), gameId, teamId, cancellationToken);
            return Results.NoContent();
        })
        .RequireAuthorization();

        group.MapPatch("/roster-members/{rosterMemberId:guid}", async Task<IResult> (Guid gameId, UpdateRosterMemberConfirmationRequestDTO request, Guid rosterMemberId, ClaimsPrincipal user, ITournamentRegistrationService registrationService, CancellationToken cancellationToken) =>
        {
            if (request.ConfirmationStatus is not RosterMemberConfirmationStatus.Confirmed)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["confirmationStatus"] = ["Only the Confirmed status is supported."] });

            return Results.Ok(await registrationService.ConfirmRosterAsync(GetAuth0UserId(user), gameId, rosterMemberId, cancellationToken));
        })
        .RequireAuthorization();

        var adminGroup = group.MapGroup("/admin")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "admin" });

        adminGroup.MapGet("/", async (Guid gameId, ITournamentRegistrationService registrationService, CancellationToken cancellationToken) =>
        {
            return await registrationService.GetAdminRegistrationsAsync(gameId, cancellationToken);
        });

        adminGroup.MapDelete("/users/{userId:guid}", async (Guid gameId, Guid userId, [FromBody] RemoveRegistrationDTO request, ClaimsPrincipal user, ITournamentRegistrationService registrationService, CancellationToken cancellationToken) =>
        {
            await registrationService.RemoveIndividualAsAdminAsync(gameId, userId, request.Reason, GetOptionalAuth0UserId(user), cancellationToken);
            return Results.NoContent();
        });

        adminGroup.MapDelete("/teams/{teamId:guid}", async (Guid gameId, Guid teamId, [FromBody] RemoveRegistrationDTO request, ClaimsPrincipal user, ITournamentRegistrationService registrationService, CancellationToken cancellationToken) =>
        {
            await registrationService.RemoveTeamAsAdminAsync(gameId, teamId, request.Reason, GetOptionalAuth0UserId(user), cancellationToken);
            return Results.NoContent();
        });

        return group;
    }

    private static string GetAuth0UserId(ClaimsPrincipal user)
    {
        var subject = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(subject))
            throw new UnauthorizedAccessException("Authenticated user id is missing.");

        return subject;
    }

    private static string? GetOptionalAuth0UserId(ClaimsPrincipal user)
    {
        return user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private static IResult? ValidateRosterUserIds(IReadOnlyList<Guid>? userIds)
    {
        string? error = null;
        if (userIds is null)
            error = "A roster user id collection is required.";
        else if (userIds.Count > SearchRequestLimits.MaximumPageSize)
            error = $"A roster cannot contain more than {SearchRequestLimits.MaximumPageSize} user ids.";
        else if (userIds.Contains(Guid.Empty))
            error = "Roster user ids cannot be empty.";
        else if (userIds.Distinct().Count() != userIds.Count)
            error = "Roster user ids must be unique.";

        return error is null
            ? null
            : Results.ValidationProblem(new Dictionary<string, string[]> { ["userIds"] = [error] });
    }
}
