using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.DTOs.GameDTOs;
using Mercurius.LAN.API.DTOs.MatchDTOs;
using Mercurius.LAN.API.Exceptions;
using Mercurius.LAN.API.Migrations;
using Mercurius.LAN.API.Models;
using Mercurius.LAN.API.Services.Files;
using Mercurius.LAN.API.Services.GameServices;
using Mercurius.LAN.API.Services.MatchServices;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Mercurius.LAN.API.Tests;

public class GameScheduleTests
{
    private static readonly DateTime PlannedStart = new(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Game_StoresScheduleConfiguration()
    {
        var game = CreateScheduledGame();

        Assert.Equal(PlannedStart, game.PlannedStartTime);
        Assert.Equal(10, game.AverageGameDurationMinutes);
        Assert.Equal(5, game.RoundBreakDurationMinutes);
    }

    [Theory]
    [InlineData(true, 10, 5, "Planned tournament start time is required.")]
    [InlineData(false, 0, 5, "Average game duration must be greater than zero.")]
    [InlineData(false, -1, 5, "Average game duration must be greater than zero.")]
    [InlineData(false, 1441, 5, "Average game duration cannot exceed 1440 minutes.")]
    [InlineData(false, 10, 0, "Round break duration must be greater than zero.")]
    [InlineData(false, 10, -1, "Round break duration must be greater than zero.")]
    [InlineData(false, 10, 241, "Round break duration cannot exceed 240 minutes.")]
    public void Game_RejectsInvalidScheduleConfiguration(
        bool missingPlannedStart,
        int averageMinutes,
        int breakMinutes,
        string expectedMessage)
    {
        var plannedStart = missingPlannedStart ? DateTime.MinValue : PlannedStart;

        var exception = Assert.Throws<ValidationException>(() =>
            CreateScheduledGame(plannedStartTime: plannedStart, averageMinutes: averageMinutes, breakMinutes: breakMinutes));

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public void Update_BlocksScheduleChangesAfterMatchesExist()
    {
        var game = CreateScheduledGame();
        game.Matches.Add(new Match { RoundNumber = 1 });

        var exception = Assert.Throws<ValidationException>(() => game.Update(
            "Schedule Change",
            BracketType.SingleElimination,
            GameFormat.BestOf1,
            GameFormat.BestOf5,
            ParticipationMode.Individual,
            "https://example.test/register",
            PlannedStart.AddHours(1),
            10,
            5));

        Assert.Equal("Schedule configuration cannot be changed once match generation has started.", exception.Message);
    }

    [Fact]
    public async Task StartGameAsync_AssignsEstimatedWindowsWithRoundBreaksAndFinalsDuration()
    {
        await using var dbContext = CreateDbContext();
        var game = CreateScheduledGame(format: GameFormat.BestOf1, finalsFormat: GameFormat.BestOf5);
        game.RegisterUser(CreateUser(1));
        game.RegisterUser(CreateUser(2));
        dbContext.Games.Add(game);
        await dbContext.SaveChangesAsync();

        var service = CreateGameService(dbContext, new FixedScheduleMatchModerator());

        await service.StartGameAsync(game.Id);

        var storedGame = await dbContext.Games
            .Include(g => g.Matches)
            .SingleAsync(g => g.Id == game.Id);
        var matches = storedGame.Matches.OrderBy(match => match.RoundNumber).ThenBy(match => match.MatchNumber).ToList();

        Assert.Equal(PlannedStart, matches[0].EstimatedStartTime);
        Assert.Equal(PlannedStart.AddMinutes(10), matches[0].EstimatedEndTime);
        Assert.Equal(PlannedStart, matches[1].EstimatedStartTime);
        Assert.Equal(PlannedStart.AddMinutes(30), matches[1].EstimatedEndTime);
        Assert.Equal(PlannedStart.AddMinutes(35), matches[2].EstimatedStartTime);
        Assert.Equal(PlannedStart.AddMinutes(85), matches[2].EstimatedEndTime);
        Assert.Equal(PlannedStart.AddMinutes(85), storedGame.EstimatedEndTime);
    }

    [Fact]
    public void ScheduleMigration_AddsFieldsWithSafeDefaults()
    {
        var migration = new TournamentScheduleEstimation();
        var operations = migration.UpOperations.ToList();

        Assert.Contains(operations.OfType<AddColumnOperation>(), operation =>
            operation.Table == "Games" &&
            operation.Name == "AverageGameDurationMinutes" &&
            Equals(operation.DefaultValue, 30));
        Assert.Contains(operations.OfType<AddColumnOperation>(), operation =>
            operation.Table == "Games" &&
            operation.Name == "RoundBreakDurationMinutes" &&
            Equals(operation.DefaultValue, 10));
        Assert.Contains(operations.OfType<AddColumnOperation>(), operation =>
            operation.Table == "Games" &&
            operation.Name == "PlannedStartTime" &&
            operation.DefaultValueSql == "CURRENT_TIMESTAMP");
        Assert.Contains(operations.OfType<AddColumnOperation>(), operation =>
            operation.Table == "Matches" &&
            operation.Name == "EstimatedStartTime" &&
            operation.IsNullable);
        Assert.Contains(operations.OfType<AddColumnOperation>(), operation =>
            operation.Table == "Matches" &&
            operation.Name == "EstimatedEndTime" &&
            operation.IsNullable);
    }

    [Fact]
    public void ResponseDtos_ExposeScheduleFields()
    {
        var game = CreateScheduledGame();
        game.EstimatedEndTime = PlannedStart.AddHours(2);
        var match = new Match
        {
            EstimatedStartTime = PlannedStart,
            EstimatedEndTime = PlannedStart.AddMinutes(10)
        };

        var gameDto = new GetGameDTO(game);
        var matchDto = new GetMatchDTO(match);

        Assert.Equal(PlannedStart, gameDto.PlannedStartTime);
        Assert.Equal(10, gameDto.AverageGameDurationMinutes);
        Assert.Equal(5, gameDto.RoundBreakDurationMinutes);
        Assert.Equal(PlannedStart.AddHours(2), gameDto.EstimatedEndTime);
        Assert.Equal(PlannedStart, matchDto.EstimatedStartTime);
        Assert.Equal(PlannedStart.AddMinutes(10), matchDto.EstimatedEndTime);
    }

    [Fact]
    public async Task RegisterAndUnregisterUserAsync_StillWorksWithScheduleFields()
    {
        await using var dbContext = CreateDbContext();
        var game = CreateScheduledGame();
        var user = CreateUser(3);
        dbContext.Games.Add(game);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var service = CreateGameService(dbContext, new FixedScheduleMatchModerator());

        var registered = await service.RegisterUserAsync(game.Id, user.Id);
        Assert.Single(registered.Users);

        var unregistered = await service.UnregisterUserAsync(game.Id, user.Id);
        Assert.Empty(unregistered.Users);
    }

    private static Game CreateScheduledGame(
        DateTime? plannedStartTime = null,
        int averageMinutes = 10,
        int breakMinutes = 5,
        GameFormat format = GameFormat.BestOf1,
        GameFormat finalsFormat = GameFormat.BestOf5)
    {
        return new Game(
            "Schedule Cup",
            BracketType.SingleElimination,
            format,
            finalsFormat,
            ParticipationMode.Individual,
            "https://example.test/register",
            plannedStartTime ?? PlannedStart,
            averageMinutes,
            breakMinutes)
        {
            Id = Guid.NewGuid()
        };
    }

    private static User CreateUser(int id)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Username = $"user{id}",
            Firstname = $"First{id}",
            Lastname = $"Last{id}",
            Email = $"user{id}@example.test"
        };
    }

    private static MercuriusDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MercuriusDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MercuriusDBContext(options);
    }

    private static GameService CreateGameService(MercuriusDBContext dbContext, IMatchModerator matchModerator)
    {
        return new GameService(dbContext, new FixedMatchModeratorFactory(matchModerator), new UnsupportedFileService());
    }

    private sealed class FixedScheduleMatchModerator : IMatchModerator
    {
        public IEnumerable<Match> GenerateMatchesForGame(Game game)
        {
            return
            [
                new Match
                {
                    GameId = game.Id,
                    RoundNumber = 1,
                    MatchNumber = 1,
                    Format = GameFormat.BestOf1,
                    ParticipationMode = game.ParticipationMode
                },
                new Match
                {
                    GameId = game.Id,
                    RoundNumber = 1,
                    MatchNumber = 2,
                    Format = GameFormat.BestOf3,
                    ParticipationMode = game.ParticipationMode
                },
                new Match
                {
                    GameId = game.Id,
                    RoundNumber = 2,
                    MatchNumber = 1,
                    Format = GameFormat.BestOf1,
                    ParticipationMode = game.ParticipationMode
                }
            ];
        }

        public void DeterminePlacements(Game game)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FixedMatchModeratorFactory(IMatchModerator matchModerator) : IMatchModeratorFactory
    {
        public IMatchModerator GetMatchModerator(BracketType bracketType)
        {
            return matchModerator;
        }
    }

    private sealed class UnsupportedFileService : IFileService
    {
        public Task<string> SaveImageAsync(IFormFile image)
        {
            throw new NotSupportedException();
        }
    }
}
