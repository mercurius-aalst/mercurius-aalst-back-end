using Mercurius.LAN.API.Models;
using Mercurius.LAN.API.Services.MatchServices.BracketTypes;

namespace Mercurius.LAN.API.Tests;

public class MatchModeratorTests
{
    [Fact]
    public void SingleElimination_GenerateMatchesForGame_KeepsIndividualParticipantsModeSafe_AndAdvancesByeWinner()
    {
        var game = new Game("Bracket", BracketType.SingleElimination, GameFormat.BestOf1, GameFormat.BestOf1, ParticipationMode.Individual, "https://example.test");
        game.Participants.Add(CreatePlayer(1));
        game.Participants.Add(CreatePlayer(2));
        game.Participants.Add(CreatePlayer(3));

        var moderator = new SingleEliminationMatchModerator();

        var matches = moderator.GenerateMatchesForGame(game).ToList();

        Assert.All(matches, match => Assert.Equal(ParticipationMode.Individual, match.ParticipationMode));
        Assert.All(matches.SelectMany(GetAssignedParticipants), participant => Assert.IsType<Player>(participant));

        var byeMatch = matches.Single(match => match.RoundNumber == 1 && (match.Participant1IsBYE || match.Participant2IsBYE));
        Assert.NotNull(byeMatch.Winner);

        var finalMatch = matches.Single(match => match.RoundNumber == 2);
        Assert.Contains(byeMatch.Winner, new[] { finalMatch.Participant1, finalMatch.Participant2 });
    }

    [Fact]
    public void DoubleElimination_GenerateMatchesForGame_KeepsTeamParticipantsModeSafe_AndPropagatesByeWinner()
    {
        var game = new Game("Bracket", BracketType.DoubleElimination, GameFormat.BestOf1, GameFormat.BestOf3, ParticipationMode.Team, "https://example.test");
        game.Participants.Add(CreateTeam(1));
        game.Participants.Add(CreateTeam(2));
        game.Participants.Add(CreateTeam(3));

        var moderator = new DoubleEliminationMatchModerator();

        var matches = moderator.GenerateMatchesForGame(game).ToList();

        Assert.All(matches, match => Assert.Equal(ParticipationMode.Team, match.ParticipationMode));
        Assert.All(matches.SelectMany(GetAssignedParticipants), participant => Assert.IsType<Team>(participant));

        var byeMatch = matches.Single(match => !match.IsLowerBracketMatch && match.RoundNumber == 1 && (match.Participant1IsBYE || match.Participant2IsBYE));
        Assert.NotNull(byeMatch.Winner);
        Assert.NotNull(byeMatch.WinnerNextMatch);
        Assert.Contains(byeMatch.Winner, new[] { byeMatch.WinnerNextMatch.Participant1, byeMatch.WinnerNextMatch.Participant2 });
    }

    private static IEnumerable<Participant> GetAssignedParticipants(Match match)
    {
        if (match.Participant1 is not null)
            yield return match.Participant1;

        if (match.Participant2 is not null)
            yield return match.Participant2;

        if (match.Winner is not null)
            yield return match.Winner;

        if (match.Loser is not null)
            yield return match.Loser;
    }

    private static Player CreatePlayer(int id)
    {
        return new Player($"user{id}", $"First{id}", $"Last{id}", $"user{id}@example.test", null, null, null)
        {
            Id = id
        };
    }

    private static User CreateUser(int id)
    {
        return new User
        {
            Id = id,
            Username = $"user{id}",
            Firstname = $"First{id}",
            Lastname = $"Last{id}",
            Email = $"user{id}@example.test"
        };
    }

    private static Team CreateTeam(int id)
    {
        var captain = CreateUser(id + 100);
        var team = new Team($"Team{id}", captain)
        {
            Id = id,
            CaptainUserId = captain.Id
        };

        return team;
    }
}
