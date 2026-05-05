using Mercurius.LAN.API.DTOs.MatchDTOs;
using Mercurius.LAN.API.Models;
using Mercurius.Shared.Exceptions;
using DataAnnotations = System.ComponentModel.DataAnnotations;

namespace Mercurius.LAN.API.Tests;

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
        match.SetParticipants(null, user2);
        match.SetParticipantBYEs(true, false);

        match.TryAssignByeWin();

        Assert.Equal(user2, match.UserWinner);
        Assert.Null(match.UserLoser);
    }

    [Fact]
    public void TryAssignByeWin_AssignsTeamWinner_WhenOnlyParticipant1Exists()
    {
        var team1 = CreateTeam(1);
        var match = new Match
        {
            ParticipationMode = ParticipationMode.Team
        };
        match.SetParticipants(team1, null);
        match.SetParticipantBYEs(false, true);

        match.TryAssignByeWin();

        Assert.Equal(team1, match.TeamWinner);
        Assert.Null(match.TeamLoser);
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
            UserWinner = winner,
            UserWinnerId = winner.Id,
            WinnerNextMatch = nextMatch
        };

        match.UpdateParticipantsNextMatch();

        Assert.Equal(winner, nextMatch.UserParticipant1);
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
            TeamWinner = winner,
            TeamWinnerId = winner.Id,
            WinnerNextMatch = nextMatch
        };

        match.UpdateParticipantsNextMatch();

        Assert.Equal(winner, nextMatch.TeamParticipant2);
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
            UserWinner = winner,
            UserWinnerId = winner.Id,
            UserLoser = loser,
            UserLoserId = loser.Id,
            LoserNextMatch = nextMatch
        };

        match.UpdateParticipantsNextMatch();

        Assert.Equal(loser, nextMatch.UserParticipant1);
    }

    [Theory]
    [InlineData(GameFormat.BestOf1, 1, 0)]
    [InlineData(GameFormat.BestOf3, 2, 1)]
    [InlineData(GameFormat.BestOf5, 3, 2)]
    public void SetScoresAndWinner_SetsUserWinnerAndLoser(GameFormat format, int participant1Score, int participant2Score)
    {
        var match = CreateIndividualMatch(format);

        match.SetScoresAndWinner(participant1Score, participant2Score);

        Assert.Equal(match.UserParticipant1, match.UserWinner);
        Assert.Equal(match.UserParticipant2, match.UserLoser);
    }

    [Fact]
    public void SetScoresAndWinner_SetsTeamWinnerAndLoser()
    {
        var match = CreateTeamMatch(GameFormat.BestOf3);

        match.SetScoresAndWinner(1, 2);

        Assert.Equal(match.TeamParticipant2, match.TeamWinner);
        Assert.Equal(match.TeamParticipant1, match.TeamLoser);
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

        var exception = Assert.Throws<ValidationException>(() => match.SetParticipants(CreateUser(1), CreateUser(2)));

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
        match.SetParticipants(user1, user2);
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
        match.SetParticipants(team1, team2);
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
            Email = $"user{id}@example.test",
            Roles = []
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
