using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Teams.Contracts;

public interface ITeamsModule
{
    Task<TeamSummary?> GetTeamSummaryAsync(
        TeamId teamId,
        CancellationToken cancellationToken = default);

    Task<TeamRosterSnapshot?> GetTeamRosterSnapshotAsync(
        TeamId teamId,
        CancellationToken cancellationToken = default);

    Task<PublicTeamProfile?> GetPublicTeamProfileAsync(
        string teamName,
        CancellationToken cancellationToken = default);

    Task<TeamRegistrationEligibility> GetRegistrationEligibilityAsync(
        TeamId teamId,
        UserId requestedBy,
        GameId gameId,
        CancellationToken cancellationToken = default);

    Task<MembershipMutationGuard> CanMutateMembershipAsync(
        TeamId teamId,
        UserId userId,
        CancellationToken cancellationToken = default);
}
