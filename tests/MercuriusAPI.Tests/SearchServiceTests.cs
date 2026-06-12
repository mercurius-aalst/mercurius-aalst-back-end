using System.Text.Json;
using System.Reflection;
using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.Models;
using Mercurius.LAN.API.Services.SearchServices;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.LAN.API.Tests;

public class SearchServiceTests
{
    [Fact]
    public async Task SearchAsync_ReturnsEmptyResults_ForShortQueries()
    {
        await using var dbContext = CreateDbContext();
        var service = new SearchService(dbContext);

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
        dbContext.Games.Add(CreateGame("Winter Alpha Cup"));
        await dbContext.SaveChangesAsync();

        var service = new SearchService(dbContext);

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

        var service = new SearchService(dbContext);

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

        var service = new SearchService(dbContext);

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

        var service = new SearchService(dbContext);

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

        var service = new SearchService(dbContext);

        var result = await service.SearchAsync("alpha", cursor: null, pageSize: 10);

        Assert.Empty(result.Results);
    }

    [Fact]
    public async Task SearchAsync_TreatsLikeWildcardsAsLiteralCharacters()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Games.AddRange(
            CreateGame("Cup 100%"),
            CreateGame("Cup 1000"));
        await dbContext.SaveChangesAsync();

        var service = new SearchService(dbContext);

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

        var service = new SearchService(dbContext);
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

        var service = new SearchService(dbContext);
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
        var service = new SearchService(dbContext);

        var buildQuery = typeof(SearchService).GetMethod("BuildPagedCandidateQuery", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var cursorType = typeof(SearchService).GetNestedType("SearchCursor", BindingFlags.NonPublic)!;

        var cursor = Activator.CreateInstance(cursorType, "alpha", 1, "alphab", 0, Guid.NewGuid().ToString())!;
        var query = (IQueryable)buildQuery.Invoke(service, ["alpha", cursor, 3])!;
        var sql = query.ToQueryString();

        Assert.Contains("UNION ALL", sql);
        Assert.Contains("LIKE", sql);
        Assert.Contains("\"NormalizedName\"", sql);
        Assert.DoesNotContain("lower(t.\"Name\")", sql);
        Assert.Contains("""u1."NormalizedLabel" > @cursor_NormalizedLabel""", sql);
        Assert.Contains("""u1."StableId" > @cursor_StableId""", sql);
        Assert.Contains("ORDER BY u1.\"RelevanceRank\", u1.\"NormalizedLabel\", u1.\"TypeOrder\", u1.\"StableId\"", sql);
        Assert.Contains("LIMIT", sql);
    }

    [Fact]
    public async Task SearchAsync_UserResults_DoNotExposePrivateFieldsOrOtherNavigationFields()
    {
        await using var dbContext = CreateDbContext();
        dbContext.Users.Add(CreateUser("alpha"));
        await dbContext.SaveChangesAsync();

        var service = new SearchService(dbContext);
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
            BracketType.SingleElimination,
            GameFormat.BestOf1,
            GameFormat.BestOf1,
            ParticipationMode.Individual,
            null)
        {
            Id = Guid.NewGuid()
        };
    }
}
