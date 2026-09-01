using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Teams.Contracts;

public interface ITeamsModule
{
    Task<TeamSummary?> GetTeamSummaryAsync(
        TeamId teamId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TeamId>> GetCaptainedTeamIdsAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<TeamRosterSnapshot?> GetTeamRosterSnapshotAsync(
        TeamId teamId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<TeamId, TeamRosterSnapshot>> GetTeamRosterSnapshotsAsync(
        IReadOnlyCollection<TeamId> teamIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<TeamId, string>> GetPublicTeamNamesByIdsAsync(
        IReadOnlyCollection<TeamId> teamIds,
        CancellationToken cancellationToken = default);

    Task<PublicTeamProfile?> GetPublicTeamProfileAsync(
        string teamName,
        CancellationToken cancellationToken = default);

    Task<TeamId?> GetPublicTeamIdByNameAsync(
        string teamName,
        CancellationToken cancellationToken = default);

    Task<TeamRegistrationEligibility> GetRegistrationEligibilityAsync(
        TeamId teamId,
        UserId requestedBy,
        TournamentId tournamentId,
        CancellationToken cancellationToken = default);

    Task<MembershipMutationGuard> CanMutateMembershipAsync(
        TeamId teamId,
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PublicTeamSearchDocument>> GetPublicTeamSearchDocumentsPageAsync(
        TeamId? afterId,
        int pageSize,
        CancellationToken cancellationToken = default);
}
