namespace Mercurius.Modules.Teams.Contracts;

public interface ITeamTournamentReadService
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

    Task<bool> IsTeamLogoReferencedAsync(
        string logoUrl,
        CancellationToken cancellationToken = default);
}
