using Mercurius.LAN.API.Data;
using Mercurius.Modules.Tournament.Application;
using Mercurius.Modules.Tournament.Application.DTOs.Tournaments;
using Mercurius.Modules.Tournament.Application.DTOs.Matches;
using Mercurius.Modules.Tournament.Application.DTOs.Participants;
using Mercurius.Modules.Tournament.Application.DTOs.Placements;
using Mercurius.Modules.Tournament.Application.DTOs.Registrations;
using Mercurius.Modules.Tournament.Application.Services;
using Mercurius.Modules.Tournament.Contracts;
using Mercurius.Modules.Identity.Contracts;
using Mercurius.Modules.Media.Contracts;
using Mercurius.Modules.Shared;
using Mercurius.Modules.Sponsorship.Contracts;
using Mercurius.Modules.Teams.Contracts;
using Platform.Eventing;

namespace Mercurius.Modules.Tournament.Tests;

internal static class TournamentTestSupport
{
    public static PublicUserDTO ToPublicUserDTO(this User user)
    {
        return new PublicUserDTO(new UserProfileSummary(
            new UserId(user.Id),
            user.Username,
            user.DisplayName,
            user.IsDeleted,
            user.DiscordId,
            user.SteamId,
            user.RiotId));
    }

    public static GetMatchDTO ToGetMatchDTO(this Match match) => TournamentDtoMapper.ToGetMatchDto(match);

    public static GetPlacementDTO ToGetPlacementDTO(
        this Placement placement,
        IReadOnlyCollection<User>? users = null,
        IReadOnlyCollection<Team>? teams = null)
    {
        var mapper = CreateMapper(users, teams);
        var context = CreateContext(users, teams, [], [placement]);
        return mapper.ToGetPlacementDto(placement, context);
    }

    public static GetTournamentDTO ToGetTournamentDTO(
        this TournamentAggregate tournament,
        IReadOnlyCollection<User>? users = null,
        IReadOnlyCollection<Team>? teams = null)
    {
        var mapper = CreateMapper(users, teams);
        return mapper.ToGetTournamentDtoAsync(tournament, CancellationToken.None).GetAwaiter().GetResult();
    }

    public static TournamentRegistrationDTO ToTournamentRegistrationDTO(
        this TournamentRegistration registration,
        IReadOnlyCollection<User>? users = null,
        IReadOnlyCollection<Team>? teams = null)
    {
        var mapper = CreateMapper(users, teams);
        return mapper.ToRegistrationDtoAsync(registration, CancellationToken.None).GetAwaiter().GetResult();
    }

    public static PublicTournamentRegistrationDTO ToPublicTournamentRegistrationDTO(
        this TournamentRegistration registration,
        IReadOnlyCollection<User>? users = null,
        IReadOnlyCollection<Team>? teams = null)
    {
        var mapper = CreateMapper(users, teams);
        var context = CreateContext(users, teams, [registration], []);
        return mapper.ToPublicRegistrationDto(registration, context);
    }

    public static TournamentDtoMapper CreateMapper(
        IReadOnlyCollection<User>? users = null,
        IReadOnlyCollection<Team>? teams = null,
        SponsorPlacementSummary? sponsorPlacement = null,
        ISponsorshipModule? sponsorshipModule = null)
    {
        return new TournamentDtoMapper(
            new RegistrationMappingContextBuilder(
                new StubIdentityModule(users ?? []),
                new StubTeamsModule(teams ?? [], users ?? [])),
            sponsorshipModule ?? new StubSponsorshipModule(sponsorPlacement));
    }

    private static RegistrationMappingContext CreateContext(
        IReadOnlyCollection<User>? users,
        IReadOnlyCollection<Team>? teams,
        IReadOnlyCollection<TournamentRegistration> registrations,
        IReadOnlyCollection<Placement> placements)
    {
        return new RegistrationMappingContextBuilder(
                new StubIdentityModule(users ?? []),
                new StubTeamsModule(teams ?? [], users ?? []))
            .BuildAsync(registrations, placements, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
    }

    public static IIdentityModule CreateIdentityModule(IReadOnlyCollection<User>? users = null) =>
        new StubIdentityModule(users ?? []);

    public static ITeamsModule CreateTeamsModule(
        IReadOnlyCollection<Team>? teams = null,
        IReadOnlyCollection<User>? users = null) =>
        new StubTeamsModule(teams ?? [], users ?? []);

    public static ISponsorshipModule CreateSponsorshipModule(SponsorPlacementSummary? sponsorPlacement = null) =>
        new StubSponsorshipModule(sponsorPlacement);

    public static RecordingTournamentRealtimePublisher CreateRealtimePublisher() => new();

    public static RecordingModuleEventPublisher CreateModuleEventPublisher() => new();

    public static StubMediaModule CreateMediaModule(string url = "images/tournament.webp") => new(url);

    internal sealed class RecordingTournamentRealtimePublisher : ITournamentRealtimePublisher
    {
        public List<TournamentRosterConfirmationChangedEvent> Events { get; } = [];

        public Task RosterConfirmationChangedAsync(
            Guid teamId,
            Guid rosterMemberId,
            Guid affectedUserId,
            string status,
            CancellationToken cancellationToken = default)
        {
            Events.Add(new TournamentRosterConfirmationChangedEvent(teamId, rosterMemberId, affectedUserId, status));
            return Task.CompletedTask;
        }
    }

    internal sealed class RecordingModuleEventPublisher : IModuleEventPublisher
    {
        public List<object> Events { get; } = [];

        public Guid Publish<TPayload>(TPayload payload, DateTime? occurredAtUtc = null)
            where TPayload : notnull
        {
            Events.Add(payload);
            return Guid.NewGuid();
        }
    }

    internal sealed class StubMediaModule(string url) : IMediaModule
    {
        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task<StoredMediaAsset> SaveImageAsync(MediaUpload upload, CancellationToken cancellationToken = default)
        {
            ReceivedCancellationToken = cancellationToken;
            return Task.FromResult(new StoredMediaAsset(url));
        }

        public Task DeleteImageAsync(string? imageUrl, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubIdentityModule(IReadOnlyCollection<User> users) : IIdentityModule
    {
        private readonly Dictionary<Guid, User> _users = users.ToDictionary(user => user.Id);

        public Task<UserProfileSummary?> GetUserProfileAsync(UserId userId, CancellationToken cancellationToken = default)
            => Task.FromResult(ToSummary(_users.GetValueOrDefault(userId.Value)));

        public Task<UserProfileSummary?> GetUserProfileByAuth0IdAsync(string auth0UserId, CancellationToken cancellationToken = default)
            => Task.FromResult(ToSummary(_users.Values.SingleOrDefault(user => user.Auth0UserId == auth0UserId)));

        public Task<PublicUserProfileSummary?> GetPublicProfileByUsernameAsync(string username, CancellationToken cancellationToken = default)
        {
            var user = _users.Values.SingleOrDefault(candidate => candidate.Username == username && !candidate.IsDeleted);
            return Task.FromResult(user is null
                ? null
                : new PublicUserProfileSummary(
                    new UserId(user.Id),
                    user.Username ?? string.Empty,
                    user.Firstname ?? string.Empty,
                    user.Lastname ?? string.Empty,
                    user.DiscordId,
                    user.SteamId,
                    user.RiotId));
        }

        public Task<IReadOnlyDictionary<UserId, UserProfileSummary>> GetUsersByIdsAsync(
            IReadOnlyCollection<UserId> userIds,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyDictionary<UserId, UserProfileSummary>>(
                userIds
                    .Where(userId => _users.ContainsKey(userId.Value))
                    .Select(userId => ToSummary(_users[userId.Value])!)
                    .ToDictionary(summary => summary.Id));
        }

        public Task<IReadOnlyList<PublicUserSearchDocument>> GetPublicUserSearchDocumentsPageAsync(
            UserId? afterId,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<PublicUserSearchDocument>>(
                _users.Values
                    .Where(user => user.IsComplete)
                    .Where(user => !afterId.HasValue || user.Id.CompareTo(afterId.Value.Value) > 0)
                    .OrderBy(user => user.Id)
                    .Take(pageSize)
                    .Select(user => new PublicUserSearchDocument(new UserId(user.Id), user.Username!))
                    .ToList());
        }

        private static UserProfileSummary? ToSummary(User? user)
        {
            return user is null
                ? null
                : new UserProfileSummary(
                    new UserId(user.Id),
                    user.Username,
                    user.DisplayName,
                    user.IsDeleted,
                    user.DiscordId,
                    user.SteamId,
                    user.RiotId);
        }
    }

    private sealed class StubTeamsModule(
        IReadOnlyCollection<Team> teams,
        IReadOnlyCollection<User> users) : ITeamsModule
    {
        private readonly Dictionary<Guid, Team> _teams = teams.ToDictionary(team => team.Id);
        private readonly Dictionary<Guid, User> _users = users.ToDictionary(user => user.Id);

        public Task<TeamSummary?> GetTeamSummaryAsync(TeamId teamId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_teams.TryGetValue(teamId.Value, out var team)
                ? new TeamSummary(new TeamId(team.Id), team.Name, team.CaptainUserId.HasValue ? new UserId(team.CaptainUserId.Value) : null, team.LogoUrl, team.IsDeleted)
                : null);
        }

        public Task<IReadOnlyList<TeamId>> GetCaptainedTeamIdsAsync(
            UserId userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TeamId>>(_teams.Values
                .Where(team => !team.IsDeleted && team.CaptainUserId == userId.Value)
                .OrderBy(team => team.Id)
                .Select(team => new TeamId(team.Id))
                .ToList());
        }

        public async Task<TeamRosterSnapshot?> GetTeamRosterSnapshotAsync(TeamId teamId, CancellationToken cancellationToken = default)
        {
            var snapshots = await GetTeamRosterSnapshotsAsync([teamId], cancellationToken);
            return snapshots.GetValueOrDefault(teamId);
        }

        public Task<IReadOnlyDictionary<TeamId, TeamRosterSnapshot>> GetTeamRosterSnapshotsAsync(
            IReadOnlyCollection<TeamId> teamIds,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyDictionary<TeamId, TeamRosterSnapshot>>(
                teamIds
                    .Where(teamId => _teams.ContainsKey(teamId.Value))
                    .Select(teamId => CreateSnapshot(_teams[teamId.Value]))
                    .ToDictionary(snapshot => snapshot.TeamId));
        }

        public Task<PublicTeamProfile?> GetPublicTeamProfileAsync(string teamName, CancellationToken cancellationToken = default)
            => Task.FromResult<PublicTeamProfile?>(null);

        public Task<TeamRegistrationEligibility> GetRegistrationEligibilityAsync(
            TeamId teamId,
            UserId requestedBy,
            TournamentId tournamentId,
            CancellationToken cancellationToken = default)
        {
            if (!_teams.TryGetValue(teamId.Value, out var team))
                return Task.FromResult(new TeamRegistrationEligibility(false, ["team_not_found"]));

            var reasons = new List<string>();
            if (team.IsDeleted)
                reasons.Add("team_deleted");
            if (team.CaptainUserId != requestedBy.Value)
                reasons.Add("captain_required");

            return Task.FromResult(new TeamRegistrationEligibility(reasons.Count == 0, reasons));
        }

        public Task<MembershipMutationGuard> CanMutateMembershipAsync(
            TeamId teamId,
            UserId userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new MembershipMutationGuard(true, []));
        }

        public Task<IReadOnlyList<PublicTeamSearchDocument>> GetPublicTeamSearchDocumentsPageAsync(
            TeamId? afterId,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<PublicTeamSearchDocument>>(
                _teams.Values
                    .Where(team => !team.IsDeleted)
                    .Where(team => !afterId.HasValue || team.Id.CompareTo(afterId.Value.Value) > 0)
                    .OrderBy(team => team.Id)
                    .Take(pageSize)
                    .Select(team => new PublicTeamSearchDocument(new TeamId(team.Id), team.Name))
                    .ToList());
        }

        private TeamRosterSnapshot CreateSnapshot(Team team)
        {
            return new TeamRosterSnapshot(
                new TeamId(team.Id),
                team.Name,
                team.CaptainUserId.HasValue ? new UserId(team.CaptainUserId.Value) : null,
                team.LogoUrl,
                team.IsDeleted,
                team.Members
                    .Select(member => _users.GetValueOrDefault(member.UserId))
                    .Where(user => user is not null)
                    .Select(user => new TeamMemberSnapshot(
                        new UserId(user!.Id),
                        user.Username,
                        user.DisplayName,
                        user.Id == team.CaptainUserId))
                    .ToList());
        }
    }

    private sealed class StubSponsorshipModule(SponsorPlacementSummary? sponsorPlacement) : ISponsorshipModule
    {
        public Task<SponsorSummary?> GetSponsorSummaryAsync(SponsorId sponsorId, CancellationToken cancellationToken = default)
            => Task.FromResult<SponsorSummary?>(null);

        public Task<IReadOnlyList<SponsorSearchDocument>> GetSponsorSearchDocumentsPageAsync(
            SponsorId? afterId,
            int pageSize,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SponsorSearchDocument>>([]);

        public Task<IReadOnlyDictionary<TournamentId, SponsorPlacementSummary>> GetSponsorPlacementsAsync(
            IReadOnlyCollection<TournamentId> tournamentIds,
            CancellationToken cancellationToken = default)
        {
            if (sponsorPlacement is null)
                return Task.FromResult<IReadOnlyDictionary<TournamentId, SponsorPlacementSummary>>(new Dictionary<TournamentId, SponsorPlacementSummary>());

            return Task.FromResult<IReadOnlyDictionary<TournamentId, SponsorPlacementSummary>>(
                new Dictionary<TournamentId, SponsorPlacementSummary> { [sponsorPlacement.TournamentId] = sponsorPlacement });
        }

        public Task<SponsorPlacementSummary?> GetSponsorPlacementAsync(TournamentId tournamentId, CancellationToken cancellationToken = default)
            => Task.FromResult(sponsorPlacement?.TournamentId == tournamentId ? sponsorPlacement : null);

        public Task ReplaceSponsorPlacementAsync(TournamentId tournamentId, SponsorPlacementInput? placement, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
