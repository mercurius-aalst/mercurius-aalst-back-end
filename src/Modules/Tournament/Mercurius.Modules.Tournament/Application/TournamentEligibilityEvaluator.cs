using Mercurius.Modules.Tournament.Domain;
using Mercurius.Modules.Tournament.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.Modules.Tournament.Application;

internal sealed class TournamentEligibilityEvaluator(ITournamentDbContext dbContext)
{
    public async Task<List<string>> GetIndividualTournamentFailuresAsync(
        TournamentAggregate tournament,
        Guid userId,
        Guid? excludedRegistrationId,
        CancellationToken cancellationToken)
    {
        var reasons = new List<string>();
        if (tournament.ParticipationMode != ParticipationMode.Individual)
            reasons.Add("not_individual_tournament");
        if (tournament.Status != TournamentStatus.Scheduled)
            reasons.Add("tournament_not_scheduled");
        if (await HasAnyParticipationAsync(tournament.Id, userId, excludedRegistrationId, cancellationToken))
            reasons.Add("duplicate_participation");
        return reasons;
    }

    public async Task<List<string>> GetTeamTournamentFailuresAsync(
        TournamentAggregate tournament,
        Guid teamId,
        Guid captainUserId,
        Guid? excludedRegistrationId,
        CancellationToken cancellationToken)
    {
        var reasons = new List<string>();
        if (tournament.ParticipationMode != ParticipationMode.Team)
            reasons.Add("not_team_tournament");
        if (tournament.Status != TournamentStatus.Scheduled)
            reasons.Add("tournament_not_scheduled");
        if (!tournament.TeamSize.HasValue || tournament.TeamSize.Value <= 0)
            reasons.Add("team_size_required");
        if (await dbContext.TournamentRegistrations.AnyAsync(
                registration =>
                    registration.TournamentId == tournament.Id &&
                    registration.TeamId == teamId &&
                    (!excludedRegistrationId.HasValue || registration.Id != excludedRegistrationId.Value),
                cancellationToken))
        {
            reasons.Add("team_already_registered");
        }

        if (await HasAnyParticipationAsync(tournament.Id, captainUserId, excludedRegistrationId, cancellationToken))
            reasons.Add("captain_duplicate_participation");

        return reasons;
    }

    public static List<string> GetRosterSizeFailures(TournamentAggregate tournament, IReadOnlyCollection<Guid> userIds)
    {
        var reasons = new List<string>();
        if (tournament.ParticipationMode == ParticipationMode.Team && tournament.TeamSize.HasValue && userIds.Distinct().Count() != tournament.TeamSize.Value)
            reasons.Add("exact_roster_size_required");
        return reasons;
    }

    private async Task<bool> HasAnyParticipationAsync(
        Guid tournamentId,
        Guid userId,
        Guid? excludedRegistrationId,
        CancellationToken cancellationToken)
    {
        return await dbContext.TournamentRegistrations.AnyAsync(
                   registration =>
                       registration.TournamentId == tournamentId &&
                       registration.UserId == userId &&
                       (!excludedRegistrationId.HasValue || registration.Id != excludedRegistrationId.Value),
                   cancellationToken)
               || await dbContext.TournamentRegistrationRosterMembers.AnyAsync(
                   member =>
                       member.TournamentId == tournamentId &&
                       member.UserId == userId &&
                       (!excludedRegistrationId.HasValue || member.TournamentRegistrationId != excludedRegistrationId.Value),
                   cancellationToken);
    }
}
