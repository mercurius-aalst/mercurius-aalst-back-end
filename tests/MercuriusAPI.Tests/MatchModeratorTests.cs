using Mercurius.LAN.API.Models;
using Mercurius.LAN.API.Services.MatchServices.BracketTypes;

namespace Mercurius.LAN.API.Tests;

public class MatchModeratorTests
{
    [Fact]
    public void SingleElimination_GenerateMatchesForGame_KeepsUsersModeSafe_AndAdvancesByeWinner()
    {
        var game = new Game("Bracket", BracketType.SingleElimination, GameFormat.BestOf1, GameFormat.BestOf1, ParticipationMode.Individual, "https://example.test");
        game.RegisteredUsers.Add(CreateUser(1));
        game.RegisteredUsers.Add(CreateUser(2));
        game.RegisteredUsers.Add(CreateUser(3));

        var moderator = new SingleEliminationMatchModerator();

        var matches = moderator.GenerateMatchesForGame(game).ToList();

        Assert.All(matches, match => Assert.Equal(ParticipationMode.Individual, match.ParticipationMode));
        Assert.All(matches, match => Assert.Null(match.TeamParticipant1));
        Assert.All(matches, match => Assert.Null(match.TeamParticipant2));

        var byeMatch = matches.Single(match => match.RoundNumber == 1 && (match.Participant1IsBYE || match.Participant2IsBYE));
        Assert.NotNull(byeMatch.UserWinner);

        var finalMatch = matches.Single(match => match.RoundNumber == 2);
        Assert.Contains(byeMatch.UserWinner, new[] { finalMatch.UserParticipant1, finalMatch.UserParticipant2 });
    }

    [Fact]
    public void DoubleElimination_GenerateMatchesForGame_KeepsTeamsModeSafe_AndPropagatesByeWinner()
    {
        var game = new Game("Bracket", BracketType.DoubleElimination, GameFormat.BestOf1, GameFormat.BestOf3, ParticipationMode.Team, "https://example.test");
        game.RegisteredTeams.Add(CreateTeam(1));
        game.RegisteredTeams.Add(CreateTeam(2));
        game.RegisteredTeams.Add(CreateTeam(3));

        var moderator = new DoubleEliminationMatchModerator();

        var matches = moderator.GenerateMatchesForGame(game).ToList();

        Assert.All(matches, match => Assert.Equal(ParticipationMode.Team, match.ParticipationMode));
        Assert.All(matches, match => Assert.Null(match.UserParticipant1));
        Assert.All(matches, match => Assert.Null(match.UserParticipant2));

        var byeMatch = matches.Single(match => !match.IsLowerBracketMatch && match.RoundNumber == 1 && (match.Participant1IsBYE || match.Participant2IsBYE));
        Assert.NotNull(byeMatch.TeamWinner);
        Assert.NotNull(byeMatch.WinnerNextMatch);
        Assert.Contains(byeMatch.TeamWinner, new[] { byeMatch.WinnerNextMatch.TeamParticipant1, byeMatch.WinnerNextMatch.TeamParticipant2 });
    }

    [Fact]
    public void RoundRobin_GenerateMatchesForGame_KeepsTeamsModeSafe()
    {
        var game = new Game("Bracket", BracketType.RoundRobin, GameFormat.BestOf1, GameFormat.BestOf1, ParticipationMode.Team, "https://example.test");
        game.RegisteredTeams.Add(CreateTeam(1));
        game.RegisteredTeams.Add(CreateTeam(2));
        game.RegisteredTeams.Add(CreateTeam(3));

        var moderator = new RoundRobinMatchModerator();

        var matches = moderator.GenerateMatchesForGame(game).ToList();

        Assert.NotEmpty(matches);
        Assert.All(matches, match => Assert.Equal(ParticipationMode.Team, match.ParticipationMode));
        Assert.All(matches, match => Assert.Null(match.UserParticipant1));
        Assert.All(matches, match => Assert.Null(match.UserParticipant2));
    }

    [Fact]
    public void SwissStage_GenerateMatchesForGame_KeepsUsersModeSafe()
    {
        var game = new Game("Bracket", BracketType.Swiss, GameFormat.BestOf1, GameFormat.BestOf3, ParticipationMode.Individual, "https://example.test");
        game.RegisteredUsers.Add(CreateUser(1));
        game.RegisteredUsers.Add(CreateUser(2));
        game.RegisteredUsers.Add(CreateUser(3));

        var moderator = new SwissStageMatchModerator();

        var matches = moderator.GenerateMatchesForGame(game).ToList();

        Assert.NotEmpty(matches);
        Assert.All(matches, match => Assert.Equal(ParticipationMode.Individual, match.ParticipationMode));
        Assert.All(matches, match => Assert.Null(match.TeamParticipant1));
        Assert.All(matches, match => Assert.Null(match.TeamParticipant2));
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
        return new Team($"Team{id}", captain)
        {
            Id = Guid.NewGuid(),
            CaptainUserId = captain.Id
        };
    }
}
