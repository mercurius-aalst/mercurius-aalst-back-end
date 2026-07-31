using Mercurius.Modules.Competition.Domain;
using Mercurius.Modules.Competition.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.Modules.Competition.Application;

internal sealed class CompetitionEligibilityEvaluator(ICompetitionDbContext dbContext)
{
    public async Task<List<string>> GetIndividualCompetitionFailuresAsync(
        Game game,
        Guid userId,
        Guid? excludedRegistrationId,
        CancellationToken cancellationToken)
    {
        var reasons = new List<string>();
        if (game.ParticipationMode != ParticipationMode.Individual)
            reasons.Add("not_individual_tournament");
        if (game.Status != GameStatus.Scheduled)
            reasons.Add("tournament_not_scheduled");
        if (await HasAnyParticipationAsync(game.Id, userId, excludedRegistrationId, cancellationToken))
            reasons.Add("duplicate_participation");
        return reasons;
    }

    public async Task<List<string>> GetTeamCompetitionFailuresAsync(
        Game game,
        Guid teamId,
        Guid captainUserId,
        Guid? excludedRegistrationId,
        CancellationToken cancellationToken)
    {
        var reasons = new List<string>();
        if (game.ParticipationMode != ParticipationMode.Team)
            reasons.Add("not_team_tournament");
        if (game.Status != GameStatus.Scheduled)
            reasons.Add("tournament_not_scheduled");
        if (!game.TeamSize.HasValue || game.TeamSize.Value <= 0)
            reasons.Add("team_size_required");
        if (await dbContext.TournamentRegistrations.AnyAsync(
                registration =>
                    registration.GameId == game.Id &&
                    registration.TeamId == teamId &&
                    (!excludedRegistrationId.HasValue || registration.Id != excludedRegistrationId.Value),
                cancellationToken))
        {
            reasons.Add("team_already_registered");
        }

        if (await HasAnyParticipationAsync(game.Id, captainUserId, excludedRegistrationId, cancellationToken))
            reasons.Add("captain_duplicate_participation");

        return reasons;
    }

    public static List<string> GetRosterSizeFailures(Game game, IReadOnlyCollection<Guid> userIds)
    {
        var reasons = new List<string>();
        if (game.ParticipationMode == ParticipationMode.Team && game.TeamSize.HasValue && userIds.Distinct().Count() != game.TeamSize.Value)
            reasons.Add("exact_roster_size_required");
        return reasons;
    }

    private async Task<bool> HasAnyParticipationAsync(
        Guid gameId,
        Guid userId,
        Guid? excludedRegistrationId,
        CancellationToken cancellationToken)
    {
        return await dbContext.TournamentRegistrations.AnyAsync(
                   registration =>
                       registration.GameId == gameId &&
                       registration.UserId == userId &&
                       (!excludedRegistrationId.HasValue || registration.Id != excludedRegistrationId.Value),
                   cancellationToken)
               || await dbContext.TournamentRegistrationRosterMembers.AnyAsync(
                   member =>
                       member.GameId == gameId &&
                       member.UserId == userId &&
                       (!excludedRegistrationId.HasValue || member.TournamentRegistrationId != excludedRegistrationId.Value),
                   cancellationToken);
    }
}
