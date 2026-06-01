using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.Tests.Contracts;

internal static class FixtureBuilders
{
    public static User CreateUser(
        string username = "playerone",
        string? firstName = "Player",
        string? lastName = "One",
        bool isDeleted = false)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Auth0UserId = $"auth0|{Guid.NewGuid():N}",
            Username = username,
            NormalizedUsername = username.ToLowerInvariant(),
            Firstname = firstName,
            Lastname = lastName,
            Email = $"{username}@test.local",
            EmailVerified = true,
            DiscordId = "discord-1",
            SteamId = "steam-1",
            RiotId = "riot-1",
            IsDeleted = isDeleted,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    public static Team CreateTeam(string name = "Team Alpha", User? captain = null)
    {
        var captainUser = captain ?? CreateUser("captain");
        return new Team(name, captainUser);
    }

    public static Game CreateGame(
        string name = "Valorant",
        ParticipationMode mode = ParticipationMode.Individual,
        BracketType bracketType = BracketType.SingleElimination,
        GameFormat format = GameFormat.BestOf1)
    {
        return new Game(name, bracketType, format, format, mode, "https://register.test.local");
    }

    public static Match CreateMatch(Game game, int round = 1, int matchNumber = 1)
    {
        return new Match
        {
            Id = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            Format = game.Format,
            BracketType = game.BracketType,
            ParticipationMode = game.ParticipationMode,
            RoundNumber = round,
            MatchNumber = matchNumber
        };
    }

    public static Sponsor CreateSponsor(string name = "Sponsor A")
    {
        return new Sponsor
        {
            Name = name,
            LogoUrl = "https://cdn.test.local/sponsor-a.png",
            InfoUrl = "https://sponsor.test.local",
            SponsorTier = SponsorTier.Gold
        };
    }

    public static Placement CreatePlacement(Game game, int place = 1)
    {
        return new Placement
        {
            Id = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            Place = place
        };
    }
}
