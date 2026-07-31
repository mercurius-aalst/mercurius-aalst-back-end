
namespace Mercurius.LAN.API.Tests;

public class MatchModeratorTests
{
    [Fact]
    public void SingleElimination_GenerateMatchesForGame_KeepsUsersModeSafe_AndAdvancesByeWinner()
    {
        var game = new Game("Bracket", BracketType.SingleElimination, GameFormat.BestOf1, GameFormat.BestOf1, ParticipationMode.Individual);
        AddIndividualRegistration(game, CreateUser(1));
        AddIndividualRegistration(game, CreateUser(2));
        AddIndividualRegistration(game, CreateUser(3));

        var moderator = new SingleEliminationMatchModerator();

        var matches = moderator.GenerateMatchesForGame(game).ToList();

        Assert.All(matches, match => Assert.Equal(ParticipationMode.Individual, match.ParticipationMode));
        Assert.All(matches, match => Assert.Null(match.TeamParticipant1Id));
        Assert.All(matches, match => Assert.Null(match.TeamParticipant2Id));

        var byeMatch = matches.Single(match => match.RoundNumber == 1 && (match.Participant1IsBYE || match.Participant2IsBYE));
        Assert.NotNull(byeMatch.UserWinnerId);

        var finalMatch = matches.Single(match => match.RoundNumber == 2);
        Assert.Contains(byeMatch.UserWinnerId, new[] { finalMatch.UserParticipant1Id, finalMatch.UserParticipant2Id });
    }

    [Fact]
    public void DoubleElimination_GenerateMatchesForGame_KeepsTeamsModeSafe_AndPropagatesByeWinner()
    {
        var game = new Game("Bracket", BracketType.DoubleElimination, GameFormat.BestOf1, GameFormat.BestOf3, ParticipationMode.Team, 1);
        AddTeamRegistration(game, CreateTeam(1));
        AddTeamRegistration(game, CreateTeam(2));
        AddTeamRegistration(game, CreateTeam(3));

        var moderator = new DoubleEliminationMatchModerator();

        var matches = moderator.GenerateMatchesForGame(game).ToList();

        Assert.All(matches, match => Assert.Equal(ParticipationMode.Team, match.ParticipationMode));
        Assert.All(matches, match => Assert.Null(match.UserParticipant1Id));
        Assert.All(matches, match => Assert.Null(match.UserParticipant2Id));

        var byeMatch = matches.Single(match => !match.IsLowerBracketMatch && match.RoundNumber == 1 && (match.Participant1IsBYE || match.Participant2IsBYE));
        Assert.NotNull(byeMatch.TeamWinnerId);
        Assert.NotNull(byeMatch.WinnerNextMatch);
        Assert.Contains(byeMatch.TeamWinnerId, new[] { byeMatch.WinnerNextMatch.TeamParticipant1Id, byeMatch.WinnerNextMatch.TeamParticipant2Id });
    }

    [Fact]
    public void RoundRobin_GenerateMatchesForGame_KeepsTeamsModeSafe()
    {
        var game = new Game("Bracket", BracketType.RoundRobin, GameFormat.BestOf1, GameFormat.BestOf1, ParticipationMode.Team, 1);
        AddTeamRegistration(game, CreateTeam(1));
        AddTeamRegistration(game, CreateTeam(2));
        AddTeamRegistration(game, CreateTeam(3));

        var moderator = new RoundRobinMatchModerator();

        var matches = moderator.GenerateMatchesForGame(game).ToList();

        Assert.NotEmpty(matches);
        Assert.All(matches, match => Assert.Equal(ParticipationMode.Team, match.ParticipationMode));
        Assert.All(matches, match => Assert.Null(match.UserParticipant1Id));
        Assert.All(matches, match => Assert.Null(match.UserParticipant2Id));
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

    private static void AddIndividualRegistration(Game game, User user)
    {
        game.TournamentRegistrations.Add(new TournamentRegistration
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

    private static void AddTeamRegistration(Game game, Team team)
    {
        game.TournamentRegistrations.Add(new TournamentRegistration
        {
            Id = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            Kind = TournamentRegistrationKind.Team,
            Status = TournamentRegistrationStatus.Active,
            RegisteredByUserId = team.CaptainUserId!.Value,
            RegisteredByUsernameAtRegistration = team.Captain!.Username ?? string.Empty,
            TeamId = team.Id,
            TeamNameAtRegistration = team.Name,
            TeamCaptainUserIdAtRegistration = team.CaptainUserId,
            TeamLogoUrlAtRegistration = team.LogoUrl
        });
    }
}
