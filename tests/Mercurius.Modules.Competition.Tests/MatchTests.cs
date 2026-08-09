using Mercurius.Modules.Competition.Application.DTOs.Matches;
using Mercurius.Modules.Shared.Exceptions;
using DataAnnotations = System.ComponentModel.DataAnnotations;

namespace Mercurius.Modules.Competition.Tests;

public class MatchTests
{
    [Fact]
    public void TryAssignByeWin_AssignsUserWinner_WhenOnlyParticipant2Exists()
    {
        var user2 = CreateUser(2);
        var match = new Match
        {
            ParticipationMode = ParticipationMode.Individual
        };
        match.SetIndividualParticipants(null, user2.Id);
        match.SetParticipantBYEs(true, false);

        match.TryAssignByeWin();

        Assert.Equal(user2.Id, match.UserWinnerId);
        Assert.Null(match.UserLoserId);
    }

    [Fact]
    public void TryAssignByeWin_AssignsTeamWinner_WhenOnlyParticipant1Exists()
    {
        var team1 = CreateTeam(1);
        var match = new Match
        {
            ParticipationMode = ParticipationMode.Team
        };
        match.SetTeamParticipants(team1.Id, null);
        match.SetParticipantBYEs(false, true);

        match.TryAssignByeWin();

        Assert.Equal(team1.Id, match.TeamWinnerId);
        Assert.Null(match.TeamLoserId);
    }

    [Fact]
    public void UpdateParticipantsNextMatch_PropagatesUserWinnerToUpperBracketSlot1_WhenMatchNumberIsOdd()
    {
        var winner = CreateUser(10);
        var nextMatch = new Match { ParticipationMode = ParticipationMode.Individual };
        var match = new Match
        {
            ParticipationMode = ParticipationMode.Individual,
            MatchNumber = 1,
            UserWinnerId = winner.Id,
            WinnerNextMatch = nextMatch
        };

        match.UpdateParticipantsNextMatch();

        Assert.Equal(winner.Id, nextMatch.UserParticipant1Id);
    }

    [Fact]
    public void UpdateParticipantsNextMatch_PropagatesTeamWinnerToLowerBracketSlot2_WhenAvailable()
    {
        var winner = CreateTeam(3);
        var nextMatch = new Match
        {
            ParticipationMode = ParticipationMode.Team,
            IsLowerBracketMatch = true
        };
        var match = new Match
        {
            ParticipationMode = ParticipationMode.Team,
            TeamWinnerId = winner.Id,
            WinnerNextMatch = nextMatch
        };

        match.UpdateParticipantsNextMatch();

        Assert.Equal(winner.Id, nextMatch.TeamParticipant2Id);
    }

    [Fact]
    public void UpdateParticipantsNextMatch_PropagatesUserLoserToLowerBracketSlot1_AfterFirstRound()
    {
        var winner = CreateUser(1);
        var loser = CreateUser(2);
        var nextMatch = new Match { ParticipationMode = ParticipationMode.Individual };
        var match = new Match
        {
            ParticipationMode = ParticipationMode.Individual,
            RoundNumber = 2,
            MatchNumber = 2,
            UserWinnerId = winner.Id,
            UserLoserId = loser.Id,
            LoserNextMatch = nextMatch
        };

        match.UpdateParticipantsNextMatch();

        Assert.Equal(loser.Id, nextMatch.UserParticipant1Id);
    }

    [Theory]
    [InlineData((int)GameFormat.BestOf1, 1, 0)]
    [InlineData((int)GameFormat.BestOf3, 2, 1)]
    [InlineData((int)GameFormat.BestOf5, 3, 2)]
    public void SetScoresAndWinner_SetsUserWinnerAndLoser(int format, int participant1Score, int participant2Score)
    {
        var match = CreateIndividualMatch((GameFormat)format);

        match.SetScoresAndWinner(participant1Score, participant2Score);

        Assert.Equal(match.UserParticipant1Id, match.UserWinnerId);
        Assert.Equal(match.UserParticipant2Id, match.UserLoserId);
    }

    [Fact]
    public void SetScoresAndWinner_SetsTeamWinnerAndLoser()
    {
        var match = CreateTeamMatch(GameFormat.BestOf3);

        match.SetScoresAndWinner(1, 2);

        Assert.Equal(match.TeamParticipant2Id, match.TeamWinnerId);
        Assert.Equal(match.TeamParticipant1Id, match.TeamLoserId);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    public void SetScoresAndWinner_ThrowsValidationException_WhenScoreIsNegative(int participant1Score, int participant2Score)
    {
        var match = CreateIndividualMatch();

        var exception = Assert.Throws<ValidationException>(() => match.SetScoresAndWinner(participant1Score, participant2Score));

        Assert.Equal("Scores cannot be negative", exception.Message);
    }

    [Fact]
    public void SetScoresAndWinner_ThrowsValidationException_WhenScoresAreEqualInBo1()
    {
        var match = CreateIndividualMatch(GameFormat.BestOf1);

        var exception = Assert.Throws<ValidationException>(() => match.SetScoresAndWinner(1, 1));

        Assert.Equal("Scores cannot be equal in Bo1 format", exception.Message);
    }

    [Fact]
    public void SetParticipants_ThrowsValidationException_WhenUsersAreAssignedToTeamMatch()
    {
        var match = new Match
        {
            ParticipationMode = ParticipationMode.Team
        };

        var exception = Assert.Throws<ValidationException>(() => match.SetIndividualParticipants(CreateUser(1).Id, CreateUser(2).Id));

        Assert.Equal("Match only accepts individual participants.", exception.Message);
    }

    [Fact]
    public void UpdateMatchDTO_FailsValidation_WhenScoresAreNegative()
    {
        var dto = new UpdateMatchDTO
        {
            Participant1Score = -1,
            Participant2Score = -2
        };
        var validationContext = new DataAnnotations.ValidationContext(dto);
        var validationResults = new List<DataAnnotations.ValidationResult>();

        var isValid = DataAnnotations.Validator.TryValidateObject(dto, validationContext, validationResults, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Equal(2, validationResults.Count);
    }

    private static Match CreateIndividualMatch(GameFormat format = GameFormat.BestOf1)
    {
        var user1 = CreateUser(1);
        var user2 = CreateUser(2);
        var match = new Match
        {
            ParticipationMode = ParticipationMode.Individual,
            Format = format
        };
        match.SetIndividualParticipants(user1.Id, user2.Id);
        return match;
    }

    private static Match CreateTeamMatch(GameFormat format = GameFormat.BestOf1)
    {
        var team1 = CreateTeam(1);
        var team2 = CreateTeam(2);
        var match = new Match
        {
            ParticipationMode = ParticipationMode.Team,
            Format = format
        };
        match.SetTeamParticipants(team1.Id, team2.Id);
        return match;
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

    private static Team CreateTeam(int id)
    {
        var captain = CreateUser(id + 100);
        return new Team($"Team {id}", captain)
        {
            Id = Guid.NewGuid(),
            CaptainUserId = captain.Id
        };
    }
}
