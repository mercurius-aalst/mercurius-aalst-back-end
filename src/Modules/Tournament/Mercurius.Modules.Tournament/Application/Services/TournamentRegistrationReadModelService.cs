using Mercurius.Modules.Tournament.Application.DTOs.Registrations;
using Mercurius.Modules.Tournament.Domain;
using Mercurius.Modules.Tournament.Infrastructure;
using Mercurius.Modules.Shared;
using Mercurius.Modules.Teams.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.Modules.Tournament.Application.Services;

internal sealed class TournamentRegistrationReadModelService(
    ITournamentDbContext dbContext,
    ITeamsModule teamsModule,
    RegistrationMappingContextBuilder contextBuilder,
    TournamentDtoMapper mapper)
{
    public async Task<CurrentUserTournamentRegistrationStateDTO> GetCurrentUserStateAsync(
        Guid userId,
        TournamentAggregate tournament,
        CancellationToken cancellationToken)
    {
        var registrations = await GetRegistrationQuery()
            .Where(registration => registration.TournamentId == tournament.Id)
            .Where(registration =>
                registration.UserId == userId ||
                registration.RosterMembers.Any(member => member.UserId == userId))
            .ToListAsync(cancellationToken);

        var captainedTeamIds = (await teamsModule.GetCaptainedTeamIdsAsync(new UserId(userId), cancellationToken))
            .Select(teamId => teamId.Value)
            .ToHashSet();

        if (captainedTeamIds.Count != 0)
        {
            var captainRegistrations = await GetRegistrationQuery()
                .Where(registration => registration.TournamentId == tournament.Id && registration.TeamId.HasValue && captainedTeamIds.Contains(registration.TeamId.Value))
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
        var currentTeam = registrations
            .Where(registration =>
                registration.Kind == TournamentRegistrationKind.Team &&
                registration.RosterMembers.Any(member => member.UserId == userId))
            .OrderByDescending(registration => registration.Status == TournamentRegistrationStatus.Active)
            .ThenByDescending(registration => registration.UpdatedAtUtc)
            .ThenBy(registration => registration.Id)
            .FirstOrDefault();
        var context = await contextBuilder.BuildAsync(registrations, [], cancellationToken);
        var captained = registrations
            .Where(registration => registration.TeamId.HasValue && captainedTeamIds.Contains(registration.TeamId.Value))
            .ToList();

        return new CurrentUserTournamentRegistrationStateDTO
        {
            TournamentId = tournament.Id,
            IndividualRegistration = individual is null ? null : mapper.ToRegistrationDto(individual, context),
            PendingRosterConfirmation = pendingRoster is null
                ? null
                : mapper.ToRosterMemberDto(pendingRoster, context),
            CurrentTeamRegistration = currentTeam is null ? null : mapper.ToRegistrationDto(currentTeam, context),
            ActiveTeamRegistration = activeTeam is null ? null : mapper.ToRegistrationDto(activeTeam, context),
            CaptainManagedRegistrations = captained
                .Select(registration => mapper.ToRegistrationDto(registration, context))
                .ToList(),
            CanRegisterIndividual = tournament.ParticipationMode == ParticipationMode.Individual &&
                                    tournament.Status == TournamentStatus.Scheduled &&
                                    individual is null &&
                                    currentTeam is null &&
                                    pendingRoster is null,
            CanConfirmRoster = pendingRoster is not null,
            CanUnregister = individual is not null || activeTeam is not null || captained.Any()
        };
    }

    public async Task<IReadOnlyList<AdminTournamentRegistrationDTO>> GetAdminRegistrationsAsync(
        Guid tournamentId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var offset = (long)(page - 1) * pageSize;
        cancellationToken.ThrowIfCancellationRequested();
        if (offset > int.MaxValue)
            return [];

        var registrations = await GetRegistrationQuery()
            .Where(registration => registration.TournamentId == tournamentId)
            .OrderBy(registration => registration.Kind)
            .ThenBy(registration => registration.Status)
            .ThenBy(registration => registration.CreatedAtUtc)
            .ThenBy(registration => registration.Id)
            .Skip((int)offset)
            .Take(pageSize)
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
