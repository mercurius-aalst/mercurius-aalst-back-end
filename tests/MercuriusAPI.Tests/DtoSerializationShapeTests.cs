using System.Text.Json;
using System.Text.Json.Serialization;
using Mercurius.LAN.API.DTOs.UserDTOs;
using Mercurius.Modules.Identity.DTOs;
using Mercurius.LAN.API.DTOs.GameDTOs;
using Mercurius.LAN.API.DTOs.MatchDTOs;
using Mercurius.LAN.API.DTOs.RegistrationDTOs;
using Mercurius.LAN.API.DTOs.SearchDTOs;
using Mercurius.LAN.API.DTOs.SponsorDTOs;
using Mercurius.LAN.API.DTOs.TeamDTOs;
using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.Tests;

public class DtoSerializationShapeTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public void TeamDtos_KeepCurrentJsonShape()
    {
        var captain = CreateUser(1);
        var member = CreateUser(2);
        var team = new Team("Alpha Squad", captain)
        {
            Id = Guid.NewGuid(),
            LogoUrl = "/images/alpha.png"
        };
        team.Members.Add(member);
        var invite = new TeamInvite
        {
            Id = Guid.NewGuid(),
            Team = team,
            TeamId = team.Id,
            User = member,
            UserId = member.Id,
            Status = TeamInviteStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(14)
        };

        AssertJsonProperties(new GetTeamDTO(team), "id", "name", "captainUserId", "logoUrl", "members");
        AssertJsonProperties(new TeamInviteDTO(invite), "id", "teamId", "userId", "status", "createdAt", "expiresAt", "respondedAt", "cancelledAt", "expiredAt");
        AssertJsonProperties(new TeamInviteSummaryDTO(invite), "id", "teamId", "teamName", "teamLogoUrl", "userId", "username", "status", "createdAt", "expiresAt");
    }

    [Fact]
    public void GameRegistrationAndRosterDtos_KeepCurrentJsonShape()
    {
        var captain = CreateUser(3);
        var member = CreateUser(4);
        var team = new Team("Roster Team", captain) { Id = Guid.NewGuid() };
        team.Members.Add(member);
        var game = CreateGame(ParticipationMode.Team, teamSize: 2);
        var registration = CreateTeamRegistration(game, team, captain, [captain, member], TournamentRegistrationStatus.Active);
        game.TournamentRegistrations.Add(registration);

        AssertJsonProperties(new GetGameDTO(game),
            "id",
            "name",
            "startTime",
            "endTime",
            "plannedStartTime",
            "averageGameDurationMinutes",
            "roundBreakDurationMinutes",
            "estimatedEndTime",
            "status",
            "bracketType",
            "format",
            "finalsFormat",
            "participationMode",
            "teamSize",
            "imageUrl",
            "placements",
            "sponsorPlacement",
            "matches",
            "registrations");
        AssertJsonProperties(new TournamentRegistrationDTO(registration), "id", "gameId", "kind", "status", "user", "team", "rosterMembers", "createdAtUtc", "updatedAtUtc");
        AssertJsonProperties(new PublicTournamentRegistrationDTO(registration), "id", "gameId", "kind", "status", "user", "team", "rosterMembers");
        AssertJsonProperties(new TournamentRosterMemberDTO
        {
            Id = Guid.NewGuid(),
            User = new PublicUserDTO(member),
            IsCaptain = false,
            ConfirmationStatus = RosterMemberConfirmationStatus.Confirmed
        }, "id", "user", "isCaptain", "confirmationStatus");
        AssertJsonProperties(new PublicTournamentRosterMemberDTO
        {
            User = new PublicUserDTO(member),
            IsCaptain = false
        }, "user", "isCaptain");

        var json = Serialize(new GetGameDTO(game));
        Assert.Contains("\"status\":\"Scheduled\"", json, StringComparison.Ordinal);
        Assert.Contains("\"participationMode\":\"Team\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void MatchSponsorSearchAndUserDtos_KeepCurrentJsonShape()
    {
        var user = CreateUser(5);
        var game = CreateGame(ParticipationMode.Individual);
        var match = new Match
        {
            Id = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            BracketType = BracketType.SingleElimination,
            Format = GameFormat.BestOf3,
            ParticipationMode = ParticipationMode.Individual,
            RoundNumber = 1,
            MatchNumber = 1,
            UserParticipant1 = user,
            UserParticipant1Id = user.Id,
            Participant1Score = 1,
            Participant2Score = 0
        };
        var sponsor = new Sponsor
        {
            Id = 7,
            Name = "Mercurius Tech",
            SponsorTier = SponsorTier.Gold,
            LogoUrl = "/images/mercurius-tech.png",
            InfoUrl = "https://example.test/mercurius-tech",
            Description = "Tournament partner"
        };
        var searchResult = new SearchResultDTO
        {
            Type = "user",
            DisplayLabel = user.Username!,
            SupportingText = "Player",
            Username = user.Username
        };

        AssertJsonProperties(new GetMatchDTO(match),
            "id",
            "startTime",
            "endTime",
            "estimatedStartTime",
            "estimatedEndTime",
            "bracketType",
            "format",
            "participationMode",
            "roundNumber",
            "matchNumber",
            "isLowerBracketMatch",
            "gameId",
            "userParticipant1Id",
            "userParticipant2Id",
            "teamParticipant1Id",
            "teamParticipant2Id",
            "participant1IsBYE",
            "participant2IsBYE",
            "userWinnerId",
            "userLoserId",
            "teamWinnerId",
            "teamLoserId",
            "participant1Score",
            "participant2Score",
            "winnerNextMatchId",
            "loserNextMatchId");
        AssertJsonProperties(new GetSponsorDTO(sponsor), "id", "name", "sponsorTier", "logoUrl", "infoUrl", "description");
        AssertJsonProperties(searchResult, "type", "displayLabel", "supportingText", "username");
        AssertJsonProperties(new GetUserDTO(user), "id", "username", "firstname", "lastname", "email", "emailVerified", "discordId", "steamId", "riotId", "displayName", "isDeleted", "createdAtUtc", "updatedAtUtc");
        AssertJsonProperties(new PublicUserProfileDTO
        {
            Username = user.Username!,
            Firstname = user.Firstname!,
            Lastname = user.Lastname!,
            DiscordId = user.DiscordId,
            SteamId = user.SteamId,
            RiotId = user.RiotId
        }, "username", "firstname", "lastname", "discordId", "steamId", "riotId");

        var json = Serialize(new GetSponsorDTO(sponsor));
        Assert.Contains("\"sponsorTier\":\"Gold\"", json, StringComparison.Ordinal);
    }

    private static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, WebJson);
    }

    private static void AssertJsonProperties<T>(T value, params string[] expectedProperties)
    {
        using var document = JsonDocument.Parse(Serialize(value));

        var actualProperties = document.RootElement
            .EnumerateObject()
            .Select(property => property.Name)
            .OrderBy(property => property, StringComparer.Ordinal)
            .ToArray();
        var expected = expectedProperties
            .OrderBy(property => property, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, actualProperties);
    }

    private static Game CreateGame(ParticipationMode participationMode, int? teamSize = null)
    {
        return new Game(
            "Contract Cup",
            BracketType.SingleElimination,
            GameFormat.BestOf1,
            GameFormat.BestOf3,
            participationMode,
            teamSize)
        {
            Id = Guid.NewGuid(),
            ImageUrl = "/images/contract-cup.png",
            PlannedStartTime = new DateTime(2026, 7, 1, 18, 0, 0, DateTimeKind.Utc),
            AverageGameDurationMinutes = 30,
            RoundBreakDurationMinutes = 10
        };
    }

    private static TournamentRegistration CreateTeamRegistration(
        Game game,
        Team team,
        User captain,
        IReadOnlyCollection<User> rosterMembers,
        TournamentRegistrationStatus status)
    {
        var registration = new TournamentRegistration
        {
            Id = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            Kind = TournamentRegistrationKind.Team,
            Status = status,
            RegisteredByUser = captain,
            RegisteredByUserId = captain.Id,
            Team = team,
            TeamId = team.Id,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        registration.RosterMembers = rosterMembers.Select(member => new TournamentRegistrationRosterMember
        {
            Id = Guid.NewGuid(),
            TournamentRegistration = registration,
            TournamentRegistrationId = registration.Id,
            Game = game,
            GameId = game.Id,
            Team = team,
            TeamId = team.Id,
            User = member,
            UserId = member.Id,
            IsCaptain = member.Id == captain.Id,
            ConfirmationStatus = member.Id == captain.Id
                ? RosterMemberConfirmationStatus.AutoConfirmed
                : RosterMemberConfirmationStatus.Confirmed
        }).ToList();

        return registration;
    }

    private static User CreateUser(int id)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Auth0UserId = $"auth0|user{id}",
            Username = $"user{id}",
            NormalizedUsername = $"user{id}",
            Firstname = $"First{id}",
            Lastname = $"Last{id}",
            Email = $"user{id}@example.test",
            EmailVerified = true,
            DiscordId = $"discord-{id}",
            SteamId = $"steam-{id}",
            RiotId = $"riot-{id}",
            CreatedAtUtc = new DateTime(2026, 1, id, 10, 0, 0, DateTimeKind.Utc),
            UpdatedAtUtc = new DateTime(2026, 1, id, 11, 0, 0, DateTimeKind.Utc)
        };
    }
}
