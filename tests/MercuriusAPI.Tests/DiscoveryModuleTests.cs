using Mercurius.LAN.API.Data;
using Mercurius.Modules.Competition.Contracts;
using Mercurius.Modules.Discovery;
using Mercurius.Modules.Discovery.Application;
using Mercurius.Modules.Discovery.Contracts;
using Mercurius.Modules.Discovery.Domain;
using Mercurius.Modules.Identity.Contracts;
using Mercurius.Modules.Shared;
using Mercurius.Modules.Sponsorship.Contracts;
using Mercurius.Modules.Teams.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Platform.Eventing;
using Platform.Extensions;

namespace Mercurius.LAN.API.Tests;

public class DiscoveryModuleTests
{
    [Fact]
    public async Task SearchAsync_UsesOnlyDiscoveryDocumentsAndPreservesPublicResultOrdering()
    {
        var sources = new DiscoverySources();
        await using var provider = CreateProvider(sources);
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MercuriusDBContext>();
        var projector = scope.ServiceProvider.GetRequiredService<SearchDocumentProjector>();
        var module = scope.ServiceProvider.GetRequiredService<IDiscoveryModule>();

        dbContext.Users.Add(new Mercurius.Modules.Identity.Domain.User
        {
            Id = Guid.NewGuid(),
            Auth0UserId = "auth0|live-only",
            Username = "alpha-live-only",
            NormalizedUsername = "alpha-live-only",
            Firstname = "Live",
            Lastname = "Only"
        });
        await projector.UpsertAsync(SearchDocumentTypes.User, Guid.NewGuid().ToString(), "alpha", "User", null, "/users/alpha", 1, DateTime.UtcNow, default);
        await projector.UpsertAsync(SearchDocumentTypes.Team, Guid.NewGuid().ToString(), "alphateam", "Team", null, "/teams/alphateam", 1, DateTime.UtcNow, default);
        var gameId = Guid.NewGuid();
        await projector.UpsertAsync(SearchDocumentTypes.Game, gameId.ToString(), "Winter Alpha Cup", "Game", null, $"/games/{gameId}", 1, DateTime.UtcNow, default);
        await dbContext.SaveChangesAsync();

        var result = await module.SearchAsync(new DiscoverySearchRequest(" ALPHA ", null, 10));

        Assert.Collection(result.Results,
            user =>
            {
                Assert.Equal("user", user.Type);
                Assert.Equal("alpha", user.Username);
                Assert.Null(user.TeamName);
                Assert.Null(user.GameId);
            },
            team =>
            {
                Assert.Equal("team", team.Type);
                Assert.Equal("alphateam", team.TeamName);
                Assert.Null(team.Username);
                Assert.Null(team.GameId);
            },
            game =>
            {
                Assert.Equal("game", game.Type);
                Assert.Equal(gameId, game.GameId);
                Assert.Null(game.Username);
                Assert.Null(game.TeamName);
            });
        Assert.DoesNotContain(result.Results, entry => entry.DisplayLabel == "alpha-live-only");
    }

    [Fact]
    public async Task ProjectionWriter_IgnoresStaleUpdatesAndKeepsDeletedDocumentsHidden()
    {
        await using var provider = CreateProvider(new DiscoverySources());
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MercuriusDBContext>();
        var projector = scope.ServiceProvider.GetRequiredService<SearchDocumentProjector>();
        var module = scope.ServiceProvider.GetRequiredService<IDiscoveryModule>();
        var userId = Guid.NewGuid().ToString();

        await projector.UpsertAsync(SearchDocumentTypes.User, userId, "alpha-new", "User", null, "/users/alpha-new", 20, DateTime.UtcNow, default);
        await projector.UpsertAsync(SearchDocumentTypes.User, userId, "alpha-old", "User", null, "/users/alpha-old", 10, DateTime.UtcNow, default);
        await dbContext.SaveChangesAsync();

        var updated = await module.SearchAsync(new DiscoverySearchRequest("alpha", null, 10));
        Assert.Equal("alpha-new", Assert.Single(updated.Results).Username);

        await projector.MarkDeletedAsync(SearchDocumentTypes.User, userId, 30, DateTime.UtcNow, default);
        await projector.UpsertAsync(SearchDocumentTypes.User, userId, "alpha-restored", "User", null, "/users/alpha-restored", 20, DateTime.UtcNow, default);
        await dbContext.SaveChangesAsync();

        var deleted = await module.SearchAsync(new DiscoverySearchRequest("alpha", null, 10));
        Assert.Empty(deleted.Results);
    }

    [Fact]
    public async Task Dispatcher_ProjectsSearchableUsersAndRemovesIncompleteProfiles()
    {
        await using var provider = CreateProvider(new DiscoverySources());
        using var scope = provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<MercuriusDBContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IModuleEventPublisher>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IModuleEventDispatcher>();
        var module = scope.ServiceProvider.GetRequiredService<IDiscoveryModule>();
        var userId = new UserId(Guid.NewGuid());

        publisher.Publish(new UserProfileChangedIntegrationEvent(
            userId,
            "alpha",
            "Alpha User",
            false,
            true,
            DateTime.UtcNow));
        await dbContext.SaveChangesAsync();
        Assert.Equal(1, await dispatcher.DispatchPendingAsync());
        Assert.Equal("alpha", Assert.Single((await module.SearchAsync(new DiscoverySearchRequest("alpha", null, 10))).Results).Username);

        publisher.Publish(new UserProfileChangedIntegrationEvent(
            userId,
            "alpha",
            "Incomplete profile",
            false,
            false,
            DateTime.UtcNow));
        await dbContext.SaveChangesAsync();
        Assert.Equal(1, await dispatcher.DispatchPendingAsync());
        Assert.Empty((await module.SearchAsync(new DiscoverySearchRequest("alpha", null, 10))).Results);
    }

    [Fact]
    public async Task RebuildJob_UsesSourceContractsAndCoalescesCurrentDocuments()
    {
        var userId = new UserId(Guid.NewGuid());
        var teamId = new TeamId(Guid.NewGuid());
        var gameId = new GameId(Guid.NewGuid());
        var sources = new DiscoverySources
        {
            Users = [new PublicUserSearchDocument(userId, "alpha-user")],
            Teams = [new PublicTeamSearchDocument(teamId, "alpha-team")],
            Games = [new GameSearchDocument(gameId, "Alpha Cup", "/images/alpha.webp")],
            Sponsors = [new SponsorSummary(new SponsorId(7), "Alpha Sponsor", SponsorTier.Gold, "/images/sponsor.webp", "https://example.test", null)]
        };
        await using var provider = CreateProvider(sources);
        using var scope = provider.CreateScope();
        var module = scope.ServiceProvider.GetRequiredService<IDiscoveryModule>();
        var rebuildService = scope.ServiceProvider.GetRequiredService<SearchIndexRebuildService>();

        var firstJob = await module.CreateSearchIndexRebuildJobAsync();
        var coalescedJob = await module.CreateSearchIndexRebuildJobAsync();
        Assert.Equal(firstJob.Id, coalescedJob.Id);
        Assert.True(await rebuildService.RunNextAsync(default));

        var completedJob = await module.GetSearchIndexRebuildJobAsync(firstJob.Id);
        Assert.Equal("completed", completedJob!.Status);
        var rebuiltResults = await module.SearchAsync(new DiscoverySearchRequest("alpha", null, 10));
        Assert.Equal(["game", "team", "user"], rebuiltResults.Results.Select(result => result.Type));

        sources.Users = [];
        sources.Teams = [];
        sources.Games = [];
        sources.Sponsors = [];
        _ = await module.CreateSearchIndexRebuildJobAsync();
        Assert.True(await rebuildService.RunNextAsync(default));

        Assert.Empty((await module.SearchAsync(new DiscoverySearchRequest("alpha", null, 10))).Results);
    }

    [Fact]
    public async Task RebuildJob_DoesNotExposeExceptionDetails()
    {
        var sources = new DiscoverySources
        {
            RebuildException = new InvalidOperationException("database-secret")
        };
        await using var provider = CreateProvider(sources);
        using var scope = provider.CreateScope();
        var module = scope.ServiceProvider.GetRequiredService<IDiscoveryModule>();
        var rebuildService = scope.ServiceProvider.GetRequiredService<SearchIndexRebuildService>();

        var job = await module.CreateSearchIndexRebuildJobAsync();
        Assert.True(await rebuildService.RunNextAsync(default));

        var failedJob = await module.GetSearchIndexRebuildJobAsync(job.Id);
        Assert.Equal("failed", failedJob!.Status);
        Assert.Equal("The rebuild failed. Check server logs for details.", failedJob.Error);
        Assert.DoesNotContain("database-secret", failedJob.Error, StringComparison.Ordinal);
    }

    private static ServiceProvider CreateProvider(DiscoverySources sources)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddDbContext<MercuriusDBContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddModuleEventing<MercuriusDBContext>();
        services.AddSingleton<IIdentityModule>(new StubIdentityModule(sources));
        services.AddSingleton<ITeamsModule>(new StubTeamsModule(sources));
        services.AddSingleton<ICompetitionModule>(new StubCompetitionModule(sources));
        services.AddSingleton<ISponsorshipModule>(new StubSponsorshipModule(sources));
        services.AddDiscoveryModule<MercuriusDBContext>(configuration);

        return services.BuildServiceProvider();
    }

    private sealed class DiscoverySources
    {
        public IReadOnlyList<PublicUserSearchDocument> Users { get; set; } = [];
        public IReadOnlyList<PublicTeamSearchDocument> Teams { get; set; } = [];
        public IReadOnlyList<GameSearchDocument> Games { get; set; } = [];
        public IReadOnlyList<SponsorSummary> Sponsors { get; set; } = [];
        public Exception? RebuildException { get; set; }
    }

    private sealed class StubIdentityModule(DiscoverySources sources) : IIdentityModule
    {
        public Task<IReadOnlyList<PublicUserSearchDocument>> GetPublicUserSearchDocumentsAsync(CancellationToken cancellationToken = default) =>
            sources.RebuildException is null
                ? Task.FromResult(sources.Users)
                : Task.FromException<IReadOnlyList<PublicUserSearchDocument>>(sources.RebuildException);
        public Task<UserProfileSummary?> GetUserProfileAsync(UserId userId, CancellationToken cancellationToken = default) => Task.FromResult<UserProfileSummary?>(null);
        public Task<UserProfileSummary?> GetUserProfileByAuth0IdAsync(string auth0UserId, CancellationToken cancellationToken = default) => Task.FromResult<UserProfileSummary?>(null);
        public Task<PublicUserProfileSummary?> GetPublicProfileByUsernameAsync(string username, CancellationToken cancellationToken = default) => Task.FromResult<PublicUserProfileSummary?>(null);
        public Task<IReadOnlyDictionary<UserId, UserProfileSummary>> GetUsersByIdsAsync(IReadOnlyCollection<UserId> userIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyDictionary<UserId, UserProfileSummary>>(new Dictionary<UserId, UserProfileSummary>());
    }

    private sealed class StubTeamsModule(DiscoverySources sources) : ITeamsModule
    {
        public Task<IReadOnlyList<PublicTeamSearchDocument>> GetPublicTeamSearchDocumentsAsync(CancellationToken cancellationToken = default) => Task.FromResult(sources.Teams);
        public Task<TeamSummary?> GetTeamSummaryAsync(TeamId teamId, CancellationToken cancellationToken = default) => Task.FromResult<TeamSummary?>(null);
        public Task<TeamRosterSnapshot?> GetTeamRosterSnapshotAsync(TeamId teamId, CancellationToken cancellationToken = default) => Task.FromResult<TeamRosterSnapshot?>(null);
        public Task<IReadOnlyDictionary<TeamId, TeamRosterSnapshot>> GetTeamRosterSnapshotsAsync(IReadOnlyCollection<TeamId> teamIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyDictionary<TeamId, TeamRosterSnapshot>>(new Dictionary<TeamId, TeamRosterSnapshot>());
        public Task<PublicTeamProfile?> GetPublicTeamProfileAsync(string teamName, CancellationToken cancellationToken = default) => Task.FromResult<PublicTeamProfile?>(null);
        public Task<TeamRegistrationEligibility> GetRegistrationEligibilityAsync(TeamId teamId, UserId requestedBy, GameId gameId, CancellationToken cancellationToken = default) => Task.FromResult(new TeamRegistrationEligibility(true, []));
        public Task<MembershipMutationGuard> CanMutateMembershipAsync(TeamId teamId, UserId userId, CancellationToken cancellationToken = default) => Task.FromResult(new MembershipMutationGuard(true, []));
    }

    private sealed class StubCompetitionModule(DiscoverySources sources) : ICompetitionModule
    {
        public Task<IReadOnlyList<GameSearchDocument>> GetGameSearchDocumentsAsync(CancellationToken cancellationToken = default) => Task.FromResult(sources.Games);
        public Task<GameSummary?> GetGameSummaryAsync(GameId gameId, CancellationToken cancellationToken = default) => Task.FromResult<GameSummary?>(null);
        public Task<TournamentConfiguration?> GetTournamentConfigurationAsync(GameId gameId, CancellationToken cancellationToken = default) => Task.FromResult<TournamentConfiguration?>(null);
        public Task<bool> IsRegistrationOpenAsync(GameId gameId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<RegistrationEligibility> CheckIndividualRegistrationEligibilityAsync(GameId gameId, UserId userId, CancellationToken cancellationToken = default) => Task.FromResult(new RegistrationEligibility(true, []));
        public Task<RegistrationEligibility> CheckTeamRegistrationEligibilityAsync(GameId gameId, TeamId teamId, UserId requestedBy, CancellationToken cancellationToken = default) => Task.FromResult(new RegistrationEligibility(true, []));
        public Task<IReadOnlyList<GameSummary>> SearchGamesAsync(string normalizedQuery, CompetitionSearchCursor? cursor, int limit, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<GameSummary>>([]);
    }

    private sealed class StubSponsorshipModule(DiscoverySources sources) : ISponsorshipModule
    {
        public Task<IReadOnlyList<SponsorSummary>> GetSponsorsAsync(CancellationToken cancellationToken = default) => Task.FromResult(sources.Sponsors);
        public Task<SponsorSummary?> GetSponsorSummaryAsync(SponsorId sponsorId, CancellationToken cancellationToken = default) => Task.FromResult<SponsorSummary?>(null);
        public Task<IReadOnlyDictionary<GameId, SponsorPlacementSummary>> GetSponsorPlacementsAsync(IReadOnlyCollection<GameId> gameIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyDictionary<GameId, SponsorPlacementSummary>>(new Dictionary<GameId, SponsorPlacementSummary>());
        public Task<SponsorPlacementSummary?> GetSponsorPlacementAsync(GameId gameId, CancellationToken cancellationToken = default) => Task.FromResult<SponsorPlacementSummary?>(null);
        public Task ReplaceSponsorPlacementAsync(GameId gameId, SponsorPlacementInput? placement, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
