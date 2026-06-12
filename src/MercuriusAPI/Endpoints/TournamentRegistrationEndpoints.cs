using Asp.Versioning;
using Mercurius.LAN.API.DTOs.RegistrationDTOs;
using Mercurius.LAN.API.Services.RegistrationServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Mercurius.LAN.API.Endpoints;

public static class TournamentRegistrationEndpoints
{
    public static RouteGroupBuilder MapTournamentRegistrationEndpoints(this WebApplication app)
    {
        var apiVersionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        var group = app.MapGroup("v{version:apiVersion}/lan/games/{gameId:guid}/registrations")
            .WithApiVersionSet(apiVersionSet)
            .MapToApiVersion(new ApiVersion(1, 0))
            .WithTags("Tournament Registrations");

        group.MapGet("/me", async (Guid gameId, ClaimsPrincipal user, ITournamentRegistrationService registrationService) =>
        {
            return await registrationService.GetCurrentUserStateAsync(GetAuth0UserId(user), gameId);
        })
        .RequireAuthorization();

        group.MapGet("/eligibility/individual", async (Guid gameId, ClaimsPrincipal user, ITournamentRegistrationService registrationService) =>
        {
            return await registrationService.CheckIndividualEligibilityAsync(GetAuth0UserId(user), gameId);
        })
        .RequireAuthorization();

        group.MapGet("/eligibility/teams/{teamId:guid}", async (Guid gameId, Guid teamId, ClaimsPrincipal user, ITournamentRegistrationService registrationService) =>
        {
            return await registrationService.CheckTeamEligibilityAsync(GetAuth0UserId(user), gameId, teamId);
        })
        .RequireAuthorization();

        group.MapPost("/eligibility/teams/{teamId:guid}/roster", async (Guid gameId, Guid teamId, SubmitTeamRosterDTO request, ClaimsPrincipal user, ITournamentRegistrationService registrationService) =>
        {
            return await registrationService.CheckRosterEligibilityAsync(GetAuth0UserId(user), gameId, teamId, request.UserIds);
        })
        .RequireAuthorization();

        group.MapPost("/individual", async (Guid gameId, ClaimsPrincipal user, ITournamentRegistrationService registrationService) =>
        {
            return await registrationService.RegisterIndividualAsync(GetAuth0UserId(user), gameId);
        })
        .RequireAuthorization();

        group.MapDelete("/individual/me", async (Guid gameId, ClaimsPrincipal user, ITournamentRegistrationService registrationService) =>
        {
            await registrationService.UnregisterIndividualAsync(GetAuth0UserId(user), gameId);
            return Results.NoContent();
        })
        .RequireAuthorization();

        group.MapPut("/teams/{teamId:guid}/roster", async (Guid gameId, Guid teamId, SubmitTeamRosterDTO request, ClaimsPrincipal user, ITournamentRegistrationService registrationService) =>
        {
            var requestWithRouteTeam = request with { TeamId = teamId };
            return await registrationService.SubmitTeamRosterAsync(GetAuth0UserId(user), gameId, requestWithRouteTeam);
        })
        .RequireAuthorization();

        group.MapDelete("/teams/{teamId:guid}", async (Guid gameId, Guid teamId, ClaimsPrincipal user, ITournamentRegistrationService registrationService) =>
        {
            await registrationService.UnregisterTeamAsync(GetAuth0UserId(user), gameId, teamId);
            return Results.NoContent();
        })
        .RequireAuthorization();

        group.MapPost("/roster-confirmations/{rosterMemberId:guid}/confirm", async (Guid rosterMemberId, ClaimsPrincipal user, ITournamentRegistrationService registrationService) =>
        {
            return await registrationService.ConfirmRosterAsync(GetAuth0UserId(user), rosterMemberId);
        })
        .RequireAuthorization();

        var adminGroup = group.MapGroup("/admin")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "admin" });

        adminGroup.MapGet("/", async (Guid gameId, ITournamentRegistrationService registrationService) =>
        {
            return await registrationService.GetAdminRegistrationsAsync(gameId);
        });

        adminGroup.MapDelete("/users/{userId:guid}", async (Guid gameId, Guid userId, [FromBody] RemoveRegistrationDTO request, ClaimsPrincipal user, ITournamentRegistrationService registrationService) =>
        {
            await registrationService.RemoveIndividualAsAdminAsync(gameId, userId, request.Reason, GetOptionalAuth0UserId(user));
            return Results.NoContent();
        });

        adminGroup.MapDelete("/teams/{teamId:guid}", async (Guid gameId, Guid teamId, [FromBody] RemoveRegistrationDTO request, ClaimsPrincipal user, ITournamentRegistrationService registrationService) =>
        {
            await registrationService.RemoveTeamAsAdminAsync(gameId, teamId, request.Reason, GetOptionalAuth0UserId(user));
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
}
