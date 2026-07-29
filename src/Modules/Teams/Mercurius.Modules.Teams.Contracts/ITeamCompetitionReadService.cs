namespace Mercurius.Modules.Teams.Contracts;

public interface ITeamCompetitionReadService
{
    Task<IReadOnlyList<PublicTeamTournamentSummary>> GetPublicTeamTournamentsAsync(
        Guid teamId,
        CancellationToken cancellationToken = default);

    Task<bool> IsUserInProtectedTournamentRosterAsync(
        Guid teamId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> IsTeamInDeleteBlockingTournamentAsync(
        Guid teamId,
        CancellationToken cancellationToken = default);
}
