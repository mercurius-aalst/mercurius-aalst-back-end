using Mercurius.LAN.API.Data;
using Mercurius.Modules.Competition.Application.DTOs.Games;
using Mercurius.Modules.Competition.Application.DTOs.Matches;
using Mercurius.Modules.Competition.Infrastructure;
using Mercurius.Modules.Shared.Exceptions;
using Mercurius.LAN.API.Migrations;
using Mercurius.Modules.Media.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Mercurius.Modules.Competition.Tests;

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

    [Fact]
    public void Game_AllowsRoundBreakDurationsAbovePreviousLimit()
    {
        var game = CreateScheduledGame(breakMinutes: 241);

        Assert.Equal(241, game.RoundBreakDurationMinutes);
    }

    [Theory]
    [InlineData(true, 10, 5, "Planned tournament start time is required.")]
    [InlineData(false, 0, 5, "Average game duration must be greater than zero.")]
    [InlineData(false, -1, 5, "Average game duration must be greater than zero.")]
    [InlineData(false, 1441, 5, "Average game duration cannot exceed 1440 minutes.")]
    [InlineData(false, 10, 0, "Round break duration must be greater than zero.")]
    [InlineData(false, 10, -1, "Round break duration must be greater than zero.")]
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
            null,
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
        dbContext.Set<Game>().Add(game);
        AddIndividualRegistration(dbContext, game, CreateUser(1));
        AddIndividualRegistration(dbContext, game, CreateUser(2));
        await dbContext.SaveChangesAsync();

        var service = CreateGameService(dbContext, new FixedScheduleMatchModerator());

        await service.StartGameAsync(game.Id);

        var storedGame = await dbContext.Set<Game>()
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
    public async Task StartGameAsync_DoesNotApplyFinalsFormatToRoundRobinLastRound()
    {
        await using var dbContext = CreateDbContext();
        var game = CreateScheduledGame(
            format: GameFormat.BestOf1,
            finalsFormat: GameFormat.BestOf5,
            bracketType: BracketType.RoundRobin);
        dbContext.Set<Game>().Add(game);
        AddIndividualRegistration(dbContext, game, CreateUser(1));
        AddIndividualRegistration(dbContext, game, CreateUser(2));
        AddIndividualRegistration(dbContext, game, CreateUser(3));
        AddIndividualRegistration(dbContext, game, CreateUser(4));
        await dbContext.SaveChangesAsync();

        var service = CreateGameService(dbContext, new RoundRobinMatchModerator());

        await service.StartGameAsync(game.Id);

        var matches = await dbContext.Set<Match>().ToListAsync();

        Assert.All(matches, match =>
            Assert.Equal(TimeSpan.FromMinutes(10), match.EstimatedEndTime - match.EstimatedStartTime));
    }

    [Fact]
    public async Task StartGameAsync_RejectsEstimatedScheduleDateOverflow()
    {
        await using var dbContext = CreateDbContext();
        var game = CreateScheduledGame(plannedStartTime: DateTime.MaxValue.AddMinutes(-5));
        dbContext.Set<Game>().Add(game);
        AddIndividualRegistration(dbContext, game, CreateUser(1));
        AddIndividualRegistration(dbContext, game, CreateUser(2));
        await dbContext.SaveChangesAsync();

        var service = CreateGameService(dbContext, new FixedScheduleMatchModerator());

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.StartGameAsync(game.Id));

        Assert.Equal("Estimated tournament schedule exceeds supported date range.", exception.Message);
    }

    [Fact]
    public async Task CreateGameAsync_ForwardsCancellationTokenToImageStorage()
    {
        await using var dbContext = CreateDbContext();
        var mediaModule = new RecordingMediaModule();
        var service = new GameService(
            new CompetitionDbContextAdapter<MercuriusDBContext>(dbContext),
            new FixedMatchModeratorFactory(new FixedScheduleMatchModerator()),
            mediaModule,
            CompetitionTestSupport.CreateSponsorshipModule(),
            CompetitionTestSupport.CreateMapper(),
            CompetitionTestSupport.CreateModuleEventPublisher());
        using var cancellationSource = new CancellationTokenSource();
        var imageBytes = new byte[] { 1, 2, 3 };
        var image = new FormFile(new MemoryStream(imageBytes), 0, imageBytes.Length, "image", "game.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };

        await service.CreateGameAsync(new CreateGameDTO
        {
            Name = "Cancellation token game",
            BracketType = Mercurius.Modules.Competition.Contracts.BracketType.SingleElimination,
            Format = Mercurius.Modules.Competition.Contracts.GameFormat.BestOf1,
            FinalsFormat = Mercurius.Modules.Competition.Contracts.GameFormat.BestOf3,
            ParticipationMode = Mercurius.Modules.Competition.Contracts.ParticipationMode.Individual,
            Image = image,
            PlannedStartTime = PlannedStart,
            AverageGameDurationMinutes = 10,
            RoundBreakDurationMinutes = 5
        }, cancellationSource.Token);

        Assert.Equal(cancellationSource.Token, mediaModule.ReceivedCancellationToken);
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

        var gameDto = game.ToGetGameDTO();
        var matchDto = match.ToGetMatchDTO();

        Assert.Equal(PlannedStart, gameDto.PlannedStartTime);
        Assert.Equal(10, gameDto.AverageGameDurationMinutes);
        Assert.Equal(5, gameDto.RoundBreakDurationMinutes);
        Assert.Equal(PlannedStart.AddHours(2), gameDto.EstimatedEndTime);
        Assert.Equal(PlannedStart, matchDto.EstimatedStartTime);
        Assert.Equal(PlannedStart.AddMinutes(10), matchDto.EstimatedEndTime);
    }

    private static Game CreateScheduledGame(
        DateTime? plannedStartTime = null,
        int averageMinutes = 10,
        int breakMinutes = 5,
        GameFormat format = GameFormat.BestOf1,
        GameFormat finalsFormat = GameFormat.BestOf5,
        BracketType bracketType = BracketType.SingleElimination)
    {
        return new Game(
            "Schedule Cup",
            bracketType,
            format,
            finalsFormat,
            ParticipationMode.Individual,
            null,
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

    private static void AddIndividualRegistration(MercuriusDBContext dbContext, Game game, User user)
    {
        dbContext.Users.Add(user);
        dbContext.Set<TournamentRegistration>().Add(new TournamentRegistration
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
        });
    }

    private static GameService CreateGameService(MercuriusDBContext dbContext, IMatchModerator matchModerator)
    {
        return new GameService(
            new CompetitionDbContextAdapter<MercuriusDBContext>(dbContext),
            new FixedMatchModeratorFactory(matchModerator),
            new UnsupportedMediaModule(),
            CompetitionTestSupport.CreateSponsorshipModule(),
            CompetitionTestSupport.CreateMapper(),
            CompetitionTestSupport.CreateModuleEventPublisher());
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
                    Format = game.FinalsFormat,
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

    private sealed class UnsupportedMediaModule : IMediaModule
    {
        public Task<StoredMediaAsset> SaveImageAsync(MediaUpload upload, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task DeleteImageAsync(string? imageUrl, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class RecordingMediaModule : IMediaModule
    {
        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task<StoredMediaAsset> SaveImageAsync(MediaUpload upload, CancellationToken cancellationToken = default)
        {
            ReceivedCancellationToken = cancellationToken;
            return Task.FromResult(new StoredMediaAsset("images/game.webp"));
        }

        public Task DeleteImageAsync(string? imageUrl, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
