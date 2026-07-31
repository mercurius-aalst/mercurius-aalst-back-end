using System.Reflection;
using System.Text.Json;
using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.Models;
using Mercurius.LAN.API.Services.SearchServices;
using Mercurius.Modules.Competition.Contracts;
using Mercurius.Modules.Shared;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.LAN.API.Tests;

public class SearchServiceTests
{
    [Fact]
    public async Task SearchAsync_ReturnsEmptyResults_ForShortQueries()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var result = await service.SearchAsync("ab", cursor: null, pageSize: 10);

        Assert.Empty(result.Results);
        Assert.False(result.HasMore);
        Assert.Null(result.NextCursor);
    }

    [Fact]
    public async Task SearchAsync_ReturnsUsersTeamsAndGames_CaseInsensitive_InDeterministicOrder()
    {
        await using var dbContext = CreateDbContext();

        dbContext.Users.Add(CreateUser("alpha"));
        dbContext.Teams.Add(CreateTeam("alphateam", CreateUser("captain-one")));
        dbContext.Set<Game>().Add(CreateGame("Winter Alpha Cup"));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.SearchAsync("  ALPHA  ", cursor: null, pageSize: 10);

        Assert.Collection(result.Results,
            user =>
            {
                Assert.Equal("user", user.Type);
                Assert.Equal("alpha", user.DisplayLabel);
                Assert.Equal("alpha", user.Username);
            },
            team =>
            {
                Assert.Equal("team", team.Type);
                Assert.Equal("alphateam", team.DisplayLabel);
                Assert.Equal("alphateam", team.TeamName);
            },
            game =>
            {
                Assert.Equal("game", game.Type);
                Assert.Equal("Winter Alpha Cup", game.DisplayLabel);
                Assert.NotNull(game.GameId);
            });
    }

    [Fact]
    public async Task SearchAsync_MatchesTeamsByPersistedNormalizedName()
    {
        await using var dbContext = CreateDbContext();
        var team = CreateTeam("Display Name", CreateUser("captain-one"));
        team.NormalizedName = "alpha-team";
        dbContext.Teams.Add(team);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, []);

        var result = await service.SearchAsync("alpha", cursor: null, pageSize: 10);

        var teamResult = Assert.Single(result.Results);
        Assert.Equal("team", teamResult.Type);
        Assert.Equal("Display Name", teamResult.DisplayLabel);
        Assert.Equal("Display Name", teamResult.TeamName);
    }

    [Fact]
    public async Task SearchAsync_SupportsCursorContinuation()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.AddRange(
            CreateUser("alpha"),
            CreateUser("alphaa"),
            CreateUser("alphab"),
            CreateUser("alphac"),
            CreateUser("alphad"));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext, Array.Empty<Game>());

        var page1 = await service.SearchAsync("alpha", cursor: null, pageSize: 2);
        Assert.Equal(2, page1.Results.Count);
        Assert.True(page1.HasMore);
        Assert.NotNull(page1.NextCursor);

        var page2 = await service.SearchAsync("alpha", page1.NextCursor, pageSize: 2);
        Assert.Equal(2, page2.Results.Count);
        Assert.True(page2.HasMore);
        Assert.NotNull(page2.NextCursor);

        var page3 = await service.SearchAsync("alpha", page2.NextCursor, pageSize: 2);
        Assert.Single(page3.Results);
        Assert.False(page3.HasMore);
        Assert.Null(page3.NextCursor);

        var combinedUsernames = page1.Results
            .Concat(page2.Results)
            .Concat(page3.Results)
            .Select(result => result.Username)
            .ToList();

        var allResults = await service.SearchAsync("alpha", cursor: null, pageSize: 10);
        Assert.Equal(allResults.Results.Select(result => result.Username), combinedUsernames);
    }

    [Fact]
    public async Task SearchAsync_ExcludesDeletedAndIncompleteUsers()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(CreateUser("alpha-valid"));
        dbContext.Users.Add(CreateUser("alpha-deleted", isDeleted: true));
        dbContext.Users.Add(CreateUser("alpha-incomplete", includeProfileNames: false));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.SearchAsync("alpha", cursor: null, pageSize: 10);

        var usernames = result.Results
            .Where(entry => entry.Type == "user")
            .Select(entry => entry.Username)
            .ToList();

        Assert.Single(usernames);
        Assert.Equal("alpha-valid", usernames[0]);
    }

    [Fact]
    public async Task SearchAsync_ExcludesUsersWithWhitespaceOnlyProfileNames()
    {
        await using var dbContext = CreateDbContext();
        var user = CreateUser("alpha-whitespace");
        user.Firstname = " ";
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.SearchAsync("alpha", cursor: null, pageSize: 10);

        Assert.Empty(result.Results);
    }

    [Fact]
    public async Task SearchAsync_TreatsLikeWildcardsAsLiteralCharacters()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Set<Game>().AddRange(
            CreateGame("Cup 100%"),
            CreateGame("Cup 1000"));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var result = await service.SearchAsync("100%", cursor: null, pageSize: 10);

        var game = Assert.Single(result.Results);
        Assert.Equal("Cup 100%", game.DisplayLabel);
    }

    [Fact]
    public async Task SearchAsync_KeysetCursor_DoesNotRepeatResultsWhenEarlierRowsAreInserted()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.AddRange(
            CreateUser("alpha"),
            CreateUser("alphab"),
            CreateUser("alphac"));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var page1 = await service.SearchAsync("alpha", cursor: null, pageSize: 2);

        dbContext.Users.Add(CreateUser("alphaa"));
        await dbContext.SaveChangesAsync();

        var page2 = await service.SearchAsync("alpha", page1.NextCursor, pageSize: 2);

        Assert.Equal(["alpha", "alphab"], page1.Results.Select(result => result.Username));
        Assert.Equal(["alphac"], page2.Results.Select(result => result.Username));
    }

    [Fact]
    public async Task SearchAsync_ResponseIncludesNullNextCursor_WhenNoMoreResultsExist()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(CreateUser("alpha"));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.SearchAsync("alpha", cursor: null, pageSize: 10);

        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Contains("\"nextCursor\":null", json);
    }

    [Fact]
    public void SearchAsync_QueryAndKeysetCursor_TranslateForPostgreSql()
    {
        var options = new DbContextOptionsBuilder<MercuriusDBContext>()
            .UseNpgsql("Host=localhost;Database=translation-only")
            .Options;
        using var dbContext = new MercuriusDBContext(options);
        var service = CreateService(dbContext, Array.Empty<Game>());

        var buildQuery = typeof(SearchService).GetMethod("BuildPagedCandidateQuery", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var cursorType = typeof(SearchService).GetNestedType("SearchCursor", BindingFlags.NonPublic)!;

        var cursor = Activator.CreateInstance(cursorType, "alpha", 1, "alphab", 0, Guid.NewGuid().ToString())!;
        var query = (IQueryable)buildQuery.Invoke(service, ["alpha", cursor, 3])!;
        var sql = query.ToQueryString();

        Assert.Contains("UNION ALL", sql);
        Assert.Contains("LIKE", sql);
        Assert.Contains("\"NormalizedName\"", sql);
        Assert.DoesNotContain("lower(t.\"Name\")", sql);
        Assert.Contains("\"NormalizedLabel\" > @cursor_NormalizedLabel", sql);
        Assert.Contains("\"StableId\" > @cursor_StableId", sql);
        Assert.Contains("ORDER BY", sql);
        Assert.Contains("\"RelevanceRank\"", sql);
        Assert.Contains("\"NormalizedLabel\"", sql);
        Assert.Contains("\"TypeOrder\"", sql);
        Assert.Contains("\"StableId\"", sql);
        Assert.Contains("LIMIT", sql);
    }

    [Fact]
    public async Task SearchAsync_UserResults_DoNotExposePrivateFieldsOrOtherNavigationFields()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(CreateUser("alpha"));
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);
        var result = await service.SearchAsync("alpha", cursor: null, pageSize: 10);
        var userResult = Assert.Single(result.Results);

        var json = JsonSerializer.Serialize(userResult, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Contains("\"username\"", json);
        Assert.DoesNotContain("\"teamName\"", json);
        Assert.DoesNotContain("\"gameId\"", json);
        Assert.DoesNotContain("email", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("firstname", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("lastname", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("discord", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("steam", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("riot", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("auth0", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("deleted", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("createdAt", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("updatedAt", json, StringComparison.OrdinalIgnoreCase);
    }

    private static SearchService CreateService(
        MercuriusDBContext dbContext,
        IReadOnlyCollection<Game>? games = null)
    {
        return new SearchService(
            dbContext,
            new StubCompetitionModule(games ?? dbContext.Set<Game>().AsNoTracking().ToList()));
    }

    private static MercuriusDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MercuriusDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MercuriusDBContext(options);
    }

    private static User CreateUser(string username, bool isDeleted = false, bool includeProfileNames = true)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Auth0UserId = $"auth0|{Guid.NewGuid():N}",
            Username = username,
            NormalizedUsername = username.ToLowerInvariant(),
            Firstname = includeProfileNames ? "First" : null,
            Lastname = includeProfileNames ? "Last" : null,
            Email = $"{username}@example.test",
            IsDeleted = isDeleted,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    private static Team CreateTeam(string name, User captain)
    {
        return new Team(name, captain)
        {
            Id = Guid.NewGuid()
        };
    }

    private static Game CreateGame(string name)
    {
        return new Game(
            name,
            Mercurius.Modules.Competition.Domain.BracketType.SingleElimination,
            Mercurius.Modules.Competition.Domain.GameFormat.BestOf1,
            Mercurius.Modules.Competition.Domain.GameFormat.BestOf1,
            Mercurius.Modules.Competition.Domain.ParticipationMode.Individual,
            null)
        {
            Id = Guid.NewGuid()
        };
    }

    private sealed class StubCompetitionModule(IReadOnlyCollection<Game> games) : ICompetitionModule
    {
        public Task<GameSummary?> GetGameSummaryAsync(GameId gameId, CancellationToken cancellationToken = default)
            => Task.FromResult<GameSummary?>(null);

        public Task<TournamentConfiguration?> GetTournamentConfigurationAsync(GameId gameId, CancellationToken cancellationToken = default)
            => Task.FromResult<TournamentConfiguration?>(null);

        public Task<bool> IsRegistrationOpenAsync(GameId gameId, CancellationToken cancellationToken = default)
            => Task.FromResult(false);

        public Task<RegistrationEligibility> CheckIndividualRegistrationEligibilityAsync(GameId gameId, UserId userId, CancellationToken cancellationToken = default)
            => Task.FromResult(new RegistrationEligibility(true, []));

        public Task<RegistrationEligibility> CheckTeamRegistrationEligibilityAsync(GameId gameId, TeamId teamId, UserId requestedBy, CancellationToken cancellationToken = default)
            => Task.FromResult(new RegistrationEligibility(true, []));

        public Task<IReadOnlyList<GameSummary>> SearchGamesAsync(string normalizedQuery, CompetitionSearchCursor? cursor, int limit, CancellationToken cancellationToken = default)
        {
            var results = games
                .Where(game => game.Name.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
                .Select(game => new
                {
                    Game = game,
                    NormalizedLabel = game.Name.ToLowerInvariant(),
                    RelevanceRank = game.Name.Equals(normalizedQuery, StringComparison.OrdinalIgnoreCase)
                        ? 0
                        : game.Name.StartsWith(normalizedQuery, StringComparison.OrdinalIgnoreCase) ? 1 : 2
                })
                .Where(candidate =>
                    cursor is null ||
                    candidate.RelevanceRank > cursor.RelevanceRank ||
                    (candidate.RelevanceRank == cursor.RelevanceRank &&
                     string.Compare(candidate.NormalizedLabel, cursor.NormalizedLabel, StringComparison.Ordinal) > 0) ||
                    (candidate.RelevanceRank == cursor.RelevanceRank &&
                     candidate.NormalizedLabel == cursor.NormalizedLabel &&
                     2 > cursor.TypeOrder) ||
                    (candidate.RelevanceRank == cursor.RelevanceRank &&
                     candidate.NormalizedLabel == cursor.NormalizedLabel &&
                     cursor.TypeOrder == 2 &&
                     candidate.Game.Id.CompareTo(cursor.StableId) > 0))
                .OrderBy(candidate => candidate.RelevanceRank)
                .ThenBy(candidate => candidate.NormalizedLabel, StringComparer.Ordinal)
                .ThenBy(candidate => candidate.Game.Id)
                .Select(game => new GameSummary(
                    new GameId(game.Game.Id),
                    game.Game.Name,
                    Mercurius.Modules.Competition.Contracts.GameStatus.Scheduled,
                    (Mercurius.Modules.Competition.Contracts.ParticipationMode)game.Game.ParticipationMode,
                    game.Game.TeamSize,
                    game.Game.PlannedStartTime,
                    game.Game.ImageUrl))
                .Take(limit)
                .ToList();

            return Task.FromResult<IReadOnlyList<GameSummary>>(results);
        }
    }
}
