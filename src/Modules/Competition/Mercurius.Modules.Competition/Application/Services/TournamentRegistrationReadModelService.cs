using Mercurius.Modules.Competition.Application.DTOs.Registrations;
using Mercurius.Modules.Competition.Domain;
using Mercurius.Modules.Competition.Infrastructure;
using Mercurius.Modules.Shared;
using Mercurius.Modules.Teams.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.Modules.Competition.Application.Services;

internal sealed class TournamentRegistrationReadModelService(
    ICompetitionDbContext dbContext,
    ITeamsModule teamsModule,
    RegistrationMappingContextBuilder contextBuilder,
    CompetitionDtoMapper mapper)
{
    public async Task<CurrentUserTournamentRegistrationStateDTO> GetCurrentUserStateAsync(
        Guid userId,
        Game game,
        CancellationToken cancellationToken)
    {
        var registrations = await GetRegistrationQuery()
            .Where(registration => registration.GameId == game.Id)
            .Where(registration =>
                registration.UserId == userId ||
                registration.RosterMembers.Any(member => member.UserId == userId))
            .ToListAsync(cancellationToken);

        var captainCandidateTeamIds = await dbContext.TournamentRegistrations
            .AsNoTracking()
            .Where(registration => registration.GameId == game.Id && registration.TeamId.HasValue)
            .Select(registration => registration.TeamId!.Value)
            .Distinct()
            .Select(teamId => new TeamId(teamId))
            .ToArrayAsync(cancellationToken);
        var captainedTeamIds = (await teamsModule.GetTeamRosterSnapshotsAsync(captainCandidateTeamIds, cancellationToken))
            .Values
            .Where(team => team.CaptainUserId?.Value == userId)
            .Select(team => team.TeamId.Value)
            .ToHashSet();

        if (captainedTeamIds.Count != 0)
        {
            var captainRegistrations = await GetRegistrationQuery()
                .Where(registration => registration.GameId == game.Id && registration.TeamId.HasValue && captainedTeamIds.Contains(registration.TeamId.Value))
                .ToListAsync(cancellationToken);

            foreach (var registration in captainRegistrations)
            {
                if (registrations.All(existing => existing.Id != registration.Id))
                    registrations.Add(registration);
            }
        }

        var individual = registrations.FirstOrDefault(registration => registration.UserId == userId);
        var pendingRoster = registrations
            .SelectMany(registration => registration.RosterMembers)
            .FirstOrDefault(member => member.UserId == userId && member.ConfirmationStatus == RosterMemberConfirmationStatus.Pending);
        var activeTeam = registrations.FirstOrDefault(registration =>
            registration.Kind == TournamentRegistrationKind.Team &&
            registration.Status == TournamentRegistrationStatus.Active &&
            registration.RosterMembers.Any(member => member.UserId == userId));
        var context = await contextBuilder.BuildAsync(registrations, [], cancellationToken);
        var captained = registrations
            .Where(registration => registration.TeamId.HasValue && captainedTeamIds.Contains(registration.TeamId.Value))
            .ToList();

        return new CurrentUserTournamentRegistrationStateDTO
        {
            GameId = game.Id,
            IndividualRegistration = individual is null ? null : mapper.ToRegistrationDto(individual, context),
            PendingRosterConfirmation = pendingRoster is null
                ? null
                : mapper.ToRosterMemberDto(pendingRoster, context),
            ActiveTeamRegistration = activeTeam is null ? null : mapper.ToRegistrationDto(activeTeam, context),
            CaptainManagedRegistrations = captained
                .Select(registration => mapper.ToRegistrationDto(registration, context))
                .ToList(),
            CanRegisterIndividual = game.ParticipationMode == ParticipationMode.Individual &&
                                    game.Status == GameStatus.Scheduled &&
                                    individual is null &&
                                    activeTeam is null &&
                                    pendingRoster is null,
            CanConfirmRoster = pendingRoster is not null,
            CanUnregister = individual is not null || activeTeam is not null || captained.Any()
        };
    }

    public async Task<IReadOnlyList<AdminTournamentRegistrationDTO>> GetAdminRegistrationsAsync(
        Guid gameId,
        CancellationToken cancellationToken)
    {
        var registrations = await GetRegistrationQuery()
            .Where(registration => registration.GameId == gameId)
            .OrderBy(registration => registration.Kind)
            .ThenBy(registration => registration.Status)
            .ThenBy(registration => registration.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return await mapper.ToAdminRegistrationDtosAsync(registrations, cancellationToken);
    }

    private IQueryable<TournamentRegistration> GetRegistrationQuery()
    {
        return dbContext.TournamentRegistrations
            .AsNoTracking()
            .AsSplitQuery()
            .Include(registration => registration.RosterMembers);
    }
}
