using Mercurius.Modules.Competition.Contracts;
using Mercurius.Modules.Identity.Contracts;
using Mercurius.Modules.Shared;
using Mercurius.Modules.Sponsorship.Contracts;
using Mercurius.Modules.Teams.Contracts;
using Platform.Eventing;

namespace Mercurius.Modules.Sponsorship.Tests;

internal static class SponsorshipCompetitionTestDoubles
{
    public static IIdentityModule CreateIdentityModule() => new EmptyIdentityModule();

    public static ITeamsModule CreateTeamsModule() => new EmptyTeamsModule();

    public static IModuleEventPublisher CreateModuleEventPublisher() => new NoopModuleEventPublisher();

    private sealed class EmptyIdentityModule : IIdentityModule
    {
        public Task<UserProfileSummary?> GetUserProfileAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<UserProfileSummary?>(null);

        public Task<UserProfileSummary?> GetUserProfileByAuth0IdAsync(string auth0UserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<UserProfileSummary?>(null);

        public Task<PublicUserProfileSummary?> GetPublicProfileByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
            Task.FromResult<PublicUserProfileSummary?>(null);

        public Task<IReadOnlyDictionary<UserId, UserProfileSummary>> GetUsersByIdsAsync(
            IReadOnlyCollection<UserId> userIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<UserId, UserProfileSummary>>(new Dictionary<UserId, UserProfileSummary>());

        public Task<IReadOnlyList<PublicUserSearchDocument>> GetPublicUserSearchDocumentsPageAsync(
            UserId? afterId,
            int pageSize,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PublicUserSearchDocument>>([]);
    }

    private sealed class EmptyTeamsModule : ITeamsModule
    {
        public Task<TeamSummary?> GetTeamSummaryAsync(TeamId teamId, CancellationToken cancellationToken = default) =>
            Task.FromResult<TeamSummary?>(null);

        public Task<TeamRosterSnapshot?> GetTeamRosterSnapshotAsync(TeamId teamId, CancellationToken cancellationToken = default) =>
            Task.FromResult<TeamRosterSnapshot?>(null);

        public Task<IReadOnlyDictionary<TeamId, TeamRosterSnapshot>> GetTeamRosterSnapshotsAsync(
            IReadOnlyCollection<TeamId> teamIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<TeamId, TeamRosterSnapshot>>(new Dictionary<TeamId, TeamRosterSnapshot>());

        public Task<PublicTeamProfile?> GetPublicTeamProfileAsync(string teamName, CancellationToken cancellationToken = default) =>
            Task.FromResult<PublicTeamProfile?>(null);

        public Task<TeamRegistrationEligibility> GetRegistrationEligibilityAsync(
            TeamId teamId,
            UserId requestedBy,
            GameId gameId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new TeamRegistrationEligibility(true, []));

        public Task<MembershipMutationGuard> CanMutateMembershipAsync(
            TeamId teamId,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new MembershipMutationGuard(true, []));

        public Task<IReadOnlyList<PublicTeamSearchDocument>> GetPublicTeamSearchDocumentsPageAsync(
            TeamId? afterId,
            int pageSize,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PublicTeamSearchDocument>>([]);
    }

    private sealed class NoopModuleEventPublisher : IModuleEventPublisher
    {
        public Guid Publish<TPayload>(TPayload payload, DateTime? occurredAtUtc = null)
            where TPayload : notnull => Guid.NewGuid();
    }
}
