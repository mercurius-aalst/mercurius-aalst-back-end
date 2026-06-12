using Mercurius.LAN.API.DTOs.GameDTOs;
using Mercurius.LAN.API.Exceptions;
using Mercurius.LAN.API.Models;
using DataAnnotations = System.ComponentModel.DataAnnotations;

namespace Mercurius.LAN.API.Tests;

public class GameTests
{
    private Game CreateGame(
        string name = "Test Game",
        BracketType bracketType = BracketType.SingleElimination,
        GameFormat format = GameFormat.BestOf1,
        GameFormat finalsFormat = GameFormat.BestOf1,
        ParticipationMode participationMode = ParticipationMode.Individual,
        int? teamSize = null)
    {
        return new Game(name, bracketType, format, finalsFormat, participationMode, teamSize ?? (participationMode == ParticipationMode.Team ? 5 : null));
    }

    [Fact]
    public void Constructor_SetsPropertiesCorrectly()
    {
        var game = CreateGame("LAN", BracketType.RoundRobin, GameFormat.BestOf3, GameFormat.BestOf5, ParticipationMode.Team, 5);

        Assert.Equal("LAN", game.Name);
        Assert.Equal(BracketType.RoundRobin, game.BracketType);
        Assert.Equal(GameFormat.BestOf3, game.Format);
        Assert.Equal(GameFormat.BestOf5, game.FinalsFormat);
        Assert.Equal(GameStatus.Scheduled, game.Status);
        Assert.Equal(ParticipationMode.Team, game.ParticipationMode);
        Assert.Equal(ParticipationMode.Team, game.ParticipationMode);
        Assert.NotNull(game.Placements);
        Assert.NotNull(game.Matches);
        Assert.NotNull(game.TournamentRegistrations);
    }

    [Fact]
    public void Update_UpdatesProperties_WhenStatusIsScheduled()
    {
        var game = CreateGame();

        game.Update("Updated", BracketType.DoubleElimination, GameFormat.BestOf3, GameFormat.BestOf5, ParticipationMode.Team, 5, game.PlannedStartTime, game.AverageGameDurationMinutes, game.RoundBreakDurationMinutes);

        Assert.Equal("Updated", game.Name);
        Assert.Equal(BracketType.DoubleElimination, game.BracketType);
        Assert.Equal(GameFormat.BestOf3, game.Format);
        Assert.Equal(GameFormat.BestOf5, game.FinalsFormat);
        Assert.Equal(ParticipationMode.Team, game.ParticipationMode);
        Assert.Equal(5, game.TeamSize);
    }

    [Theory]
    [InlineData(GameStatus.InProgress)]
    [InlineData(GameStatus.Completed)]
    public void Update_ThrowsException_WhenStatusIsInProgressOrCompleted(GameStatus status)
    {
        var game = CreateGame();
        game.Status = status;

        Assert.Throws<ValidationException>(() =>
            game.Update("New", BracketType.Swiss, GameFormat.BestOf1, GameFormat.BestOf3, ParticipationMode.Team, 5, game.PlannedStartTime, game.AverageGameDurationMinutes, game.RoundBreakDurationMinutes));
    }

    [Fact]
    public void Update_ThrowsException_WhenParticipationModeChangesAfterMatchesExist()
    {
        var game = CreateGame();
        game.Matches.Add(new Match());

        var ex = Assert.Throws<ValidationException>(() =>
            game.Update("New", BracketType.Swiss, GameFormat.BestOf1, GameFormat.BestOf3, ParticipationMode.Team, 5, game.PlannedStartTime, game.AverageGameDurationMinutes, game.RoundBreakDurationMinutes));

        Assert.Equal("Participation mode cannot be changed once registration or match generation has started.", ex.Message);
    }

    [Fact]
    public void Update_ThrowsException_WhenParticipationModeChangesAfterRegistrationsExist()
    {
        var game = CreateGame();
        var user = CreateUser(1);
        game.TournamentRegistrations.Add(new TournamentRegistration
        {
            Id = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            Kind = TournamentRegistrationKind.Individual,
            Status = TournamentRegistrationStatus.Active,
            RegisteredByUser = user,
            RegisteredByUserId = user.Id,
            User = user,
            UserId = user.Id
        });

        var ex = Assert.Throws<ValidationException>(() =>
            game.Update("New", BracketType.Swiss, GameFormat.BestOf1, GameFormat.BestOf3, ParticipationMode.Team, 2, game.PlannedStartTime, game.AverageGameDurationMinutes, game.RoundBreakDurationMinutes));

        Assert.Equal("Participation mode cannot be changed once registration or match generation has started.", ex.Message);
    }

    [Fact]
    public void Cancel_SetsStatusToCanceled_WhenNotCompleted()
    {
        var game = CreateGame();
        game.Status = GameStatus.InProgress;

        game.Cancel();

        Assert.Equal(GameStatus.Canceled, game.Status);
    }

    [Fact]
    public void Cancel_ThrowsValidationException_WhenStatusIsCompleted()
    {
        var game = CreateGame();
        game.Status = GameStatus.Completed;

        Assert.Throws<ValidationException>(() => game.Cancel());
    }

    [Fact]
    public void Start_SetsStatusAndStartTime_WhenScheduledAndEnoughParticipants()
    {
        var game = CreateGame();
        AddIndividualRegistration(game, CreateUser(1));
        AddIndividualRegistration(game, CreateUser(2));

        game.Start();

        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.True(game.StartTime <= DateTime.UtcNow && game.StartTime > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void Start_ThrowsException_WhenNotScheduled()
    {
        var game = CreateGame();
        game.Status = GameStatus.InProgress;
        AddIndividualRegistration(game, CreateUser(1));
        AddIndividualRegistration(game, CreateUser(2));

        Assert.Throws<ValidationException>(() => game.Start());
    }

    [Fact]
    public void Start_ThrowsException_WhenNotEnoughParticipants()
    {
        var game = CreateGame();
        AddIndividualRegistration(game, CreateUser(1));

        Assert.Throws<ValidationException>(() => game.Start());
    }

    [Fact]
    public void Complete_SetsStatusAndEndTime_WhenInProgress()
    {
        var game = CreateGame();
        game.Status = GameStatus.InProgress;

        game.Complete();

        Assert.Equal(GameStatus.Completed, game.Status);
        Assert.True(game.EndTime <= DateTime.UtcNow && game.EndTime > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public void Complete_ThrowsException_WhenNotInProgress()
    {
        var game = CreateGame();
        game.Status = GameStatus.Scheduled;

        Assert.Throws<ValidationException>(() => game.Complete());
    }

    [Theory]
    [InlineData(GameStatus.Completed)]
    [InlineData(GameStatus.Canceled)]
    public void Reset_SetsStatusAndClearsCollections_WhenCompletedOrCanceled(GameStatus status)
    {
        var game = CreateGame();
        game.Status = status;
        game.StartTime = DateTime.UtcNow;
        game.EndTime = DateTime.UtcNow;
        game.Matches.Add(new Match());

        game.Reset();

        Assert.Equal(GameStatus.Scheduled, game.Status);
        Assert.Equal(DateTime.MinValue, game.StartTime);
        Assert.Equal(DateTime.MinValue, game.EndTime);
        Assert.Empty(game.Matches);
    }

    [Fact]
    public void Reset_ThrowsException_WhenNotCompletedOrCanceled()
    {
        var game = CreateGame();
        game.Status = GameStatus.InProgress;

        Assert.Throws<ValidationException>(() => game.Reset());
    }

    [Fact]
    public void CreateGameDTO_FailsValidation_WhenParticipationModeIsMissing()
    {
        var dto = new CreateGameDTO
        {
            Name = "Test Game"
        };
        var validationContext = new DataAnnotations.ValidationContext(dto);
        var validationResults = new List<DataAnnotations.ValidationResult>();

        var isValid = DataAnnotations.Validator.TryValidateObject(dto, validationContext, validationResults, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(CreateGameDTO.ParticipationMode)));
    }

    [Fact]
    public void CreateGameDTO_FailsValidation_WhenRequiredFieldsAreMissing()
    {
        var dto = new CreateGameDTO();
        var validationContext = new DataAnnotations.ValidationContext(dto);
        var validationResults = new List<DataAnnotations.ValidationResult>();

        var isValid = DataAnnotations.Validator.TryValidateObject(dto, validationContext, validationResults, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(CreateGameDTO.Name)));
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(CreateGameDTO.ParticipationMode)));
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(CreateGameDTO.Image)));
    }

    private static User CreateUser(int id)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Username = "user" + id,
            Firstname = "First",
            Lastname = "Last",
            Email = $"user{id}@example.com"
        };
    }

    private static void AddIndividualRegistration(Game game, User user)
    {
        game.TournamentRegistrations.Add(new TournamentRegistration
        {
            Id = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            Kind = TournamentRegistrationKind.Individual,
            Status = TournamentRegistrationStatus.Active,
            RegisteredByUser = user,
            RegisteredByUserId = user.Id,
            User = user,
            UserId = user.Id
        });
    }

    private static Team CreateTeam(int id)
    {
        var captain = CreateUser(id);
        return new Team($"Team {id}", captain)
        {
            Id = Guid.NewGuid()
        };
    }
}
