using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.Exceptions;
using Mercurius.LAN.API.Models;
using Mercurius.LAN.API.Services.SearchServices;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit.Sdk;

namespace Mercurius.LAN.API.Tests;

public class SearchServicePostgresIntegrationTests
{
    private const string ConnectionStringEnvironmentVariable = "MERCURIUS_TEST_POSTGRES";

    [Fact]
    [Trait("Category", "PostgreSqlIntegration")]
    public async Task SearchAsync_KeysetCursor_ReturnsEveryCandidateInDatabaseOrder()
    {
        await using var fixture = await PostgresSearchFixture.CreateAsync();
        var dbContext = fixture.DbContext;

        dbContext.Users.AddRange(
            CreateUser("alp", "00000000-0000-0000-0000-000000000010"),
            CreateUser("alpha", "00000000-0000-0000-0000-000000000020"),
            CreateUser("xalp", "00000000-0000-0000-0000-000000000030"));
        dbContext.Teams.AddRange(
            CreateTeam("alpha", "captainone", "00000000-0000-0000-0000-000000000040"),
            CreateTeam("alpine", "captaintwo", "00000000-0000-0000-0000-000000000050"));
        dbContext.Games.AddRange(
            CreateGame("alpha", "00000000-0000-0000-0000-000000000060"),
            CreateGame("alpha", "00000000-0000-0000-0000-000000000070"),
            CreateGame("Alphabet Cup", "00000000-0000-0000-0000-000000000080"),
            CreateGame("Cup Alp", "00000000-0000-0000-0000-000000000090"),
            CreateGame("Cup 100%", "00000000-0000-0000-0000-000000000100"),
            CreateGame("Cup 1000", "00000000-0000-0000-0000-000000000110"),
            CreateGame(@"Mode A_B\C", "00000000-0000-0000-0000-000000000120"),
            CreateGame(@"Mode AXB\C", "00000000-0000-0000-0000-000000000130"));
        await dbContext.SaveChangesAsync();

        var service = new SearchService(dbContext);
        var results = new List<string>();
        string? cursor = null;
        string? firstCursor = null;

        do
        {
            var page = await service.SearchAsync("alp", cursor, pageSize: 3);
            results.AddRange(page.Results.Select(ToResultKey));
            firstCursor ??= page.NextCursor;
            cursor = page.NextCursor;
        }
        while (cursor is not null);

        Assert.Equal(
        [
            "user:alp",
            "user:alpha",
            "team:alpha",
            "game:00000000-0000-0000-0000-000000000060",
            "game:00000000-0000-0000-0000-000000000070",
            "game:00000000-0000-0000-0000-000000000080",
            "team:alpine",
            "game:00000000-0000-0000-0000-000000000090",
            "user:xalp"
        ], results);
        Assert.Equal(results.Count, results.Distinct().Count());

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.SearchAsync("bet", firstCursor, pageSize: 3));
        await Assert.ThrowsAsync<ValidationException>(() =>
            service.SearchAsync("alp", "not-base64", pageSize: 3));

        var percentResult = await service.SearchAsync("100%", cursor: null, pageSize: 3);
        Assert.Equal(["Cup 100%"], percentResult.Results.Select(result => result.DisplayLabel));

        var escapedResult = await service.SearchAsync(@"a_b\c", cursor: null, pageSize: 3);
        Assert.Equal([@"Mode A_B\C"], escapedResult.Results.Select(result => result.DisplayLabel));
    }

    private static string ToResultKey(DTOs.SearchDTOs.SearchResultDTO result)
    {
        return result.Type switch
        {
            "user" => $"user:{result.Username}",
            "team" => $"team:{result.TeamName}",
            "game" => $"game:{result.GameId:D}",
            _ => throw new InvalidOperationException($"Unsupported search result type '{result.Type}'.")
        };
    }

    private static User CreateUser(string username, string id)
    {
        return new User
        {
            Id = Guid.Parse(id),
            Auth0UserId = $"auth0|{Guid.NewGuid():N}",
            Username = username,
            NormalizedUsername = username.ToLowerInvariant(),
            Firstname = "First",
            Lastname = "Last",
            Email = $"{username}@example.test",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    private static Team CreateTeam(string name, string captainUsername, string id)
    {
        var captain = CreateUser(captainUsername, Guid.NewGuid().ToString());
        return new Team(name, captain)
        {
            Id = Guid.Parse(id)
        };
    }

    private static Game CreateGame(string name, string id)
    {
        return new Game(
            name,
            BracketType.SingleElimination,
            GameFormat.BestOf1,
            GameFormat.BestOf1,
            ParticipationMode.Individual,
            "https://example.test/register")
        {
            Id = Guid.Parse(id)
        };
    }

    private sealed class PostgresSearchFixture : IAsyncDisposable
    {
        private PostgresSearchFixture(MercuriusDBContext dbContext)
        {
            DbContext = dbContext;
        }

        public MercuriusDBContext DbContext { get; }

        public static async Task<PostgresSearchFixture> CreateAsync()
        {
            var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw SkipException.ForSkip(
                    $"Set {ConnectionStringEnvironmentVariable} to run PostgreSQL integration tests.");
            }

            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);
            if (!connectionStringBuilder.Database.Contains("test", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"{ConnectionStringEnvironmentVariable} must target a dedicated test database.");
            }

            var options = new DbContextOptionsBuilder<MercuriusDBContext>()
                .UseNpgsql(connectionString)
                .Options;
            var dbContext = new MercuriusDBContext(options);
            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.EnsureCreatedAsync();
            return new PostgresSearchFixture(dbContext);
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.Database.EnsureDeletedAsync();
            await DbContext.DisposeAsync();
        }
    }
}
