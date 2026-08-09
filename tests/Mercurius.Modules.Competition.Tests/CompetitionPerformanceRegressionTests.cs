using System.Reflection;
using Mercurius.LAN.API.Data;
using Mercurius.Modules.Competition;
using Mercurius.Modules.Competition.Application;
using Mercurius.Modules.Competition.Application.DTOs.Games;
using Mercurius.Modules.Competition.Application.Services;
using Mercurius.Modules.Competition.Domain;
using Mercurius.Modules.Competition.Infrastructure;
using Mercurius.Modules.Identity.Contracts;
using Mercurius.Modules.Media.Contracts;
using Mercurius.Modules.Shared;
using Mercurius.Modules.Shared.Exceptions;
using Mercurius.Modules.Sponsorship.Contracts;
using Mercurius.Modules.Teams.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Platform.Eventing;

namespace Mercurius.Modules.Competition.Tests;

public class CompetitionPerformanceRegressionTests
{
    [Fact]
    public async Task GetAllGamesAsync_ReturnsContractCompatibleSummary_WhileDetailRetainsFullGraph()
    {
        await using var dbContext = CreateDbContext();
        var user = CreateUser("Alpha");
        var game = CreateGame("Summary vs detail");
        game.Matches.Add(new Match
        {
            GameId = game.Id,
            RoundNumber = 1,
            MatchNumber = 1,
            Format = GameFormat.BestOf1,
            ParticipationMode = ParticipationMode.Individual
        });
        game.Placements.Add(new Placement
        {
            Place = 1,
            Users =
            [
                new PlacementUser { UserId = user.Id }
            ]
        });
        game.TournamentRegistrations.Add(new TournamentRegistration
        {
            GameId = game.Id,
            Kind = TournamentRegistrationKind.Individual,
            Status = TournamentRegistrationStatus.Active,
            RegisteredByUserId = user.Id,
            RegisteredByUsernameAtRegistration = user.Username ?? string.Empty,
            UserId = user.Id,
            UsernameAtRegistration = user.Username
        });

        dbContext.Users.Add(user);
        dbContext.Set<Game>().Add(game);
        await dbContext.SaveChangesAsync();

        var sponsorPlacement = CreateSponsorPlacement(game.Id);
        var service = CreateGameService(dbContext, [user], sponsorPlacement);

        var listItem = Assert.Single(await service.GetAllGamesAsync());
        var detailItem = await service.GetGameByIdAsync(game.Id);

        Assert.Equal(game.Id, listItem.Id);
        Assert.Empty(listItem.Matches);
        Assert.Empty(listItem.Placements);
        Assert.Empty(listItem.Registrations);
        Assert.Equal(sponsorPlacement.Headline, listItem.SponsorPlacement?.Headline);

        Assert.Single(detailItem.Matches);
        Assert.Single(detailItem.Placements);
        Assert.Single(detailItem.Registrations);
        Assert.Equal(user.Username, detailItem.Registrations.Single().User?.Username);
        Assert.Equal(sponsorPlacement.Headline, detailItem.SponsorPlacement?.Headline);
    }

    [Fact]
    public async Task StartGameAsync_LoadsPersistedRegistrationsBeforeCheckingParticipantCount()
    {
        await using var dbContext = CreateDbContext();
        var firstUser = CreateUser("start-first");
        var secondUser = CreateUser("start-second");
        var game = CreateGame("Persisted registrations");
        game.TournamentRegistrations.Add(CreateActiveIndividualRegistration(game, firstUser));
        game.TournamentRegistrations.Add(CreateActiveIndividualRegistration(game, secondUser));
        dbContext.Users.AddRange(firstUser, secondUser);
        dbContext.Set<Game>().Add(game);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var service = CreateGameService(dbContext, [firstUser, secondUser], sponsorPlacement: null);

        await service.StartGameAsync(game.Id);

        Assert.Equal(GameStatus.InProgress, await dbContext.Set<Game>()
            .Where(candidate => candidate.Id == game.Id)
            .Select(candidate => candidate.Status)
            .SingleAsync());
    }

    [Fact]
    public async Task UpdateGameAsync_RejectsParticipationModeChangeWhenPersistedRegistrationsExist()
    {
        await using var dbContext = CreateDbContext();
        var user = CreateUser("update-player");
        var game = CreateGame("Protected tournament configuration");
        game.TournamentRegistrations.Add(CreateActiveIndividualRegistration(game, user));
        dbContext.Users.Add(user);
        dbContext.Set<Game>().Add(game);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();

        var service = CreateGameService(dbContext, [user], sponsorPlacement: null);
        var update = new UpdateGameDTO
        {
            Name = game.Name,
            BracketType = Mercurius.Modules.Competition.Contracts.BracketType.SingleElimination,
            Format = Mercurius.Modules.Competition.Contracts.GameFormat.BestOf1,
            FinalsFormat = Mercurius.Modules.Competition.Contracts.GameFormat.BestOf3,
            ParticipationMode = Mercurius.Modules.Competition.Contracts.ParticipationMode.Team,
            TeamSize = 2,
            PlannedStartTime = game.PlannedStartTime,
            AverageGameDurationMinutes = game.AverageGameDurationMinutes,
            RoundBreakDurationMinutes = game.RoundBreakDurationMinutes
        };

        await Assert.ThrowsAsync<ValidationException>(() => service.UpdateGameAsync(game.Id, update));
    }

    [Fact]
    public void GameService_ListQuery_StaysLean()
    {
        using var dbContext = CreateTranslationDbContext();
        var service = CreateGameService(dbContext, [], sponsorPlacement: null);

        var listQuery = (IQueryable)GetNonPublicMethod(typeof(GameService), "CreateGameListQuery")
            .Invoke(service, [])!;

        var listSql = listQuery.ToQueryString();

        Assert.DoesNotContain("TournamentRegistrations", listSql, StringComparison.Ordinal);
        Assert.DoesNotContain("RosterMembers", listSql, StringComparison.Ordinal);
        Assert.DoesNotContain("Matches", listSql, StringComparison.Ordinal);
        Assert.DoesNotContain("Placements", listSql, StringComparison.Ordinal);
        Assert.DoesNotContain("JOIN", listSql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RegistrationMappingContextBuilder_LoadsUsersBeforeTeams()
    {
        var identityModule = new SequencedIdentityModule();
        var teamsModule = new SequencedTeamsModule();
        var builder = new RegistrationMappingContextBuilder(identityModule, teamsModule);
        var registration = new TournamentRegistration
        {
            UserId = Guid.NewGuid(),
            TeamId = Guid.NewGuid()
        };

        var buildTask = builder.BuildAsync([registration], [], CancellationToken.None);

        Assert.Equal(1, identityModule.CallCount);
        Assert.Equal(0, teamsModule.CallCount);

        identityModule.Complete();
        await teamsModule.WaitForCallAsync();
        teamsModule.Complete();

        await buildTask;
    }

    [Fact]
    public async Task CompetitionDtoMapper_LoadsRegistrationContextBeforeSponsorPlacements()
    {
        var identityModule = new SequencedIdentityModule();
        var teamsModule = new SequencedTeamsModule();
        var sponsorshipModule = new SequencedSponsorshipModule();
        var mapper = new CompetitionDtoMapper(
            new RegistrationMappingContextBuilder(identityModule, teamsModule),
            sponsorshipModule);
        var userId = Guid.NewGuid();
        var game = CreateGame("Sequenced mapper");
        game.TournamentRegistrations.Add(new TournamentRegistration
        {
            GameId = game.Id,
            Kind = TournamentRegistrationKind.Individual,
            Status = TournamentRegistrationStatus.Active,
            RegisteredByUserId = userId,
            RegisteredByUsernameAtRegistration = "alpha",
            UserId = userId,
            UsernameAtRegistration = "alpha"
        });

        var mappingTask = mapper.ToGetGameDtosAsync([game], CancellationToken.None);

        Assert.Equal(1, identityModule.CallCount);
        Assert.Equal(0, sponsorshipModule.CallCount);

        identityModule.Complete();
        await teamsModule.WaitForCallAsync();
        teamsModule.Complete();
        await sponsorshipModule.WaitForCallAsync();
        sponsorshipModule.Complete();

        await mappingTask;
    }

    [Fact]
    public void SearchGamesAsync_UsesLowerNamePredicate_ForFunctionalTrigramIndexAlignment()
    {
        using var dbContext = CreateTranslationDbContext();
        var facade = new CompetitionModuleFacade(
            new CompetitionDbContextAdapter<MercuriusDBContext>(dbContext),
            CompetitionTestSupport.CreateTeamsModule(),
            new CompetitionEligibilityEvaluator(new CompetitionDbContextAdapter<MercuriusDBContext>(dbContext)));

        var buildQuery = GetNonPublicMethod(typeof(CompetitionModuleFacade), "BuildGameSearchQuery");
        var query = (IQueryable)buildQuery.Invoke(facade, ["alpha"])!;
        var sql = query.ToQueryString();

        Assert.Contains("lower(", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"Name\"", sql, StringComparison.Ordinal);
        Assert.Contains("LIKE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ILIKE", sql, StringComparison.OrdinalIgnoreCase);
    }

    private static MethodInfo GetNonPublicMethod(Type type, string name) =>
        type.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException($"Missing method {name}.");

    private static GameService CreateGameService(
        MercuriusDBContext dbContext,
        IReadOnlyCollection<User> users,
        SponsorPlacementSummary? sponsorPlacement)
    {
        return new GameService(
            new CompetitionDbContextAdapter<MercuriusDBContext>(dbContext),
            new FixedMatchModeratorFactory(),
            new StubMediaModule(),
            new StaticSponsorshipModule(sponsorPlacement),
            CompetitionTestSupport.CreateMapper(users, sponsorPlacement: sponsorPlacement),
            CompetitionTestSupport.CreateModuleEventPublisher());
    }

    private static MercuriusDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MercuriusDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MercuriusDBContext(options);
    }

    private static MercuriusDBContext CreateTranslationDbContext()
    {
        var options = new DbContextOptionsBuilder<MercuriusDBContext>()
            .UseNpgsql("Host=localhost;Database=translation-only")
            .Options;

        return new MercuriusDBContext(options);
    }

    private static Game CreateGame(string name) => new(
        name,
        BracketType.SingleElimination,
        GameFormat.BestOf1,
        GameFormat.BestOf3,
        ParticipationMode.Individual,
        null,
        DateTime.UtcNow,
        30,
        10);

    private static TournamentRegistration CreateActiveIndividualRegistration(Game game, User user) => new()
    {
        Id = Guid.NewGuid(),
        Game = game,
        GameId = game.Id,
        Kind = TournamentRegistrationKind.Individual,
        Status = TournamentRegistrationStatus.Active,
        RegisteredByUserId = user.Id,
        RegisteredByUsernameAtRegistration = user.Username ?? string.Empty,
        UserId = user.Id,
        UsernameAtRegistration = user.Username
    };

    private static User CreateUser(string username)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Auth0UserId = $"auth0|{username}",
            Username = username,
            NormalizedUsername = username.ToLowerInvariant(),
            Firstname = username,
            Lastname = "Player",
            Email = $"{username}@example.com"
        };
    }

    private static SponsorPlacementSummary CreateSponsorPlacement(Guid gameId)
    {
        return new SponsorPlacementSummary(
            new SponsorPlacementId(1),
            new GameId(gameId),
            new SponsorSummary(
                new SponsorId(1),
                "Sponsor",
                SponsorTier.Gold,
                "logo.png",
                "https://example.com",
                "desc"),
            SponsorContext.TournamentPartner,
            "Headline",
            "Support",
            1);
    }

    private sealed class FixedMatchModeratorFactory : IMatchModeratorFactory
    {
        public IMatchModerator GetMatchModerator(BracketType bracketType) => new NoOpMatchModerator();
    }

    private sealed class NoOpMatchModerator : IMatchModerator
    {
        public IEnumerable<Match> GenerateMatchesForGame(Game game) => [];

        public void DeterminePlacements(Game game)
        {
        }
    }

    private sealed class StubMediaModule : IMediaModule
    {
        public Task<StoredMediaAsset> SaveImageAsync(MediaUpload upload, CancellationToken cancellationToken = default) =>
            Task.FromResult(new StoredMediaAsset("image.png"));

        public Task DeleteImageAsync(string? imageUrl, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StaticSponsorshipModule(SponsorPlacementSummary? sponsorPlacement) : ISponsorshipModule
    {
        public Task<SponsorSummary?> GetSponsorSummaryAsync(SponsorId sponsorId, CancellationToken cancellationToken = default) =>
            Task.FromResult<SponsorSummary?>(null);

        public Task<IReadOnlyList<SponsorSearchDocument>> GetSponsorSearchDocumentsPageAsync(
            SponsorId? afterId,
            int pageSize,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SponsorSearchDocument>>([]);

        public Task<IReadOnlyDictionary<GameId, SponsorPlacementSummary>> GetSponsorPlacementsAsync(
            IReadOnlyCollection<GameId> gameIds,
            CancellationToken cancellationToken = default)
        {
            if (sponsorPlacement is null)
                return Task.FromResult<IReadOnlyDictionary<GameId, SponsorPlacementSummary>>(new Dictionary<GameId, SponsorPlacementSummary>());

            return Task.FromResult<IReadOnlyDictionary<GameId, SponsorPlacementSummary>>(
                new Dictionary<GameId, SponsorPlacementSummary> { [sponsorPlacement.GameId] = sponsorPlacement });
        }

        public Task<SponsorPlacementSummary?> GetSponsorPlacementAsync(GameId gameId, CancellationToken cancellationToken = default) =>
            Task.FromResult(sponsorPlacement?.GameId == gameId ? sponsorPlacement : null);

        public Task ReplaceSponsorPlacementAsync(GameId gameId, SponsorPlacementInput? placement, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class SequencedIdentityModule : IIdentityModule
    {
        private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int CallCount { get; private set; }

        public void Complete() => _completion.TrySetResult();

        public async Task<IReadOnlyDictionary<UserId, UserProfileSummary>> GetUsersByIdsAsync(
            IReadOnlyCollection<UserId> userIds,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            await _completion.Task.WaitAsync(cancellationToken);
            return new Dictionary<UserId, UserProfileSummary>();
        }

        public Task<UserProfileSummary?> GetUserProfileAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<UserProfileSummary?>(null);

        public Task<UserProfileSummary?> GetUserProfileByAuth0IdAsync(string auth0UserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<UserProfileSummary?>(null);

        public Task<PublicUserProfileSummary?> GetPublicProfileByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
            Task.FromResult<PublicUserProfileSummary?>(null);

        public Task<IReadOnlyList<PublicUserSearchDocument>> GetPublicUserSearchDocumentsPageAsync(
            UserId? afterId,
            int pageSize,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PublicUserSearchDocument>>([]);
    }

    private sealed class SequencedTeamsModule : ITeamsModule
    {
        private readonly TaskCompletionSource _called = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int CallCount { get; private set; }

        public Task WaitForCallAsync() => _called.Task;

        public void Complete() => _completion.TrySetResult();

        public async Task<IReadOnlyDictionary<TeamId, TeamRosterSnapshot>> GetTeamRosterSnapshotsAsync(
            IReadOnlyCollection<TeamId> teamIds,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            _called.TrySetResult();
            await _completion.Task.WaitAsync(cancellationToken);
            return new Dictionary<TeamId, TeamRosterSnapshot>();
        }

        public Task<TeamSummary?> GetTeamSummaryAsync(TeamId teamId, CancellationToken cancellationToken = default) =>
            Task.FromResult<TeamSummary?>(null);

        public Task<TeamRosterSnapshot?> GetTeamRosterSnapshotAsync(TeamId teamId, CancellationToken cancellationToken = default) =>
            Task.FromResult<TeamRosterSnapshot?>(null);

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

    private sealed class SequencedSponsorshipModule : ISponsorshipModule
    {
        private readonly TaskCompletionSource _called = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int CallCount { get; private set; }

        public Task WaitForCallAsync() => _called.Task;

        public void Complete() => _completion.TrySetResult();

        public async Task<IReadOnlyDictionary<GameId, SponsorPlacementSummary>> GetSponsorPlacementsAsync(
            IReadOnlyCollection<GameId> gameIds,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            _called.TrySetResult();
            await _completion.Task.WaitAsync(cancellationToken);
            return new Dictionary<GameId, SponsorPlacementSummary>();
        }

        public Task<SponsorSummary?> GetSponsorSummaryAsync(SponsorId sponsorId, CancellationToken cancellationToken = default) =>
            Task.FromResult<SponsorSummary?>(null);

        public Task<IReadOnlyList<SponsorSearchDocument>> GetSponsorSearchDocumentsPageAsync(
            SponsorId? afterId,
            int pageSize,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SponsorSearchDocument>>([]);

        public Task<SponsorPlacementSummary?> GetSponsorPlacementAsync(GameId gameId, CancellationToken cancellationToken = default) =>
            Task.FromResult<SponsorPlacementSummary?>(null);

        public Task ReplaceSponsorPlacementAsync(GameId gameId, SponsorPlacementInput? placement, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
