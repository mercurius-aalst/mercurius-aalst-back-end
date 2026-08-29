using System.Text.Json;
using System.Text.Json.Serialization;
using Mercurius.Modules.Identity.DTOs;
using Mercurius.Modules.Tournament.Application.DTOs.Tournaments;
using Mercurius.Modules.Tournament.Application.DTOs.Matches;
using Mercurius.Modules.Tournament.Application.DTOs.Registrations;
using Mercurius.Modules.Discovery.Contracts;
using Mercurius.Modules.Sponsorship.Application.DTOs;
using Mercurius.Modules.Sponsorship.Domain;
using Mercurius.Modules.Teams.DTOs;

namespace Mercurius.Modules.Tournament.Tests;

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
        var team = new Team("Alpha Squad", captain.Id)
        {
            Id = Guid.NewGuid(),
            LogoUrl = "/images/alpha.png"
        };
        team.AddMember(captain.Id);
        team.AddMember(member.Id);
        var invite = new TeamInvite
        {
            Id = Guid.NewGuid(),
            Team = team,
            TeamId = team.Id,
            UserId = member.Id,
            Status = TeamInviteStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(14)
        };

        AssertJsonProperties(new GetTeamDTO(), "id", "name", "captainUserId", "logoUrl", "members");
        AssertJsonProperties(new TeamInviteDTO(invite), "id", "teamId", "userId", "status", "createdAt", "expiresAt", "respondedAt", "cancelledAt", "expiredAt");
        AssertJsonProperties(new TeamInviteSummaryDTO(), "id", "teamId", "teamName", "teamLogoUrl", "userId", "username", "status", "createdAt", "expiresAt");
    }

    [Fact]
    public void TournamentRegistrationAndRosterDtos_KeepCurrentJsonShape()
    {
        var captain = CreateUser(3);
        var member = CreateUser(4);
        var team = new Team("Roster Team", captain.Id) { Id = Guid.NewGuid() };
        team.AddMember(captain.Id);
        team.AddMember(member.Id);
        var tournament = CreateTournament(ParticipationMode.Team, teamSize: 2);
        var registration = CreateTeamRegistration(tournament, team, captain, [captain, member], TournamentRegistrationStatus.Active);
        tournament.TournamentRegistrations.Add(registration);

        AssertJsonProperties(tournament.ToGetTournamentDTO([captain, member], [team]),
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
        AssertJsonProperties(registration.ToTournamentRegistrationDTO([captain, member], [team]), "id", "tournamentId", "kind", "status", "user", "team", "rosterMembers", "createdAtUtc", "updatedAtUtc");
        AssertJsonProperties(registration.ToPublicTournamentRegistrationDTO([captain, member], [team]), "id", "tournamentId", "kind", "status", "user", "team", "rosterMembers");
        AssertJsonProperties(new CurrentUserTournamentRegistrationStateDTO
        {
            TournamentId = tournament.Id,
            CurrentTeamRegistration = registration.ToTournamentRegistrationDTO([captain, member], [team]),
            ActiveTeamRegistration = registration.ToTournamentRegistrationDTO([captain, member], [team])
        },
            "tournamentId",
            "individualRegistration",
            "pendingRosterConfirmation",
            "currentTeamRegistration",
            "activeTeamRegistration",
            "captainManagedRegistrations",
            "canRegisterIndividual",
            "canConfirmRoster",
            "canUnregister");
        AssertJsonProperties(new TournamentRosterMemberDTO
        {
            Id = Guid.NewGuid(),
            User = member.ToPublicUserDTO(),
            IsCaptain = false,
            ConfirmationStatus = Mercurius.Modules.Tournament.Contracts.RosterMemberConfirmationStatus.Confirmed
        }, "id", "user", "isCaptain", "confirmationStatus");
        AssertJsonProperties(new PublicTournamentRosterMemberDTO
        {
            User = member.ToPublicUserDTO(),
            IsCaptain = false
        }, "user", "isCaptain");

        var json = Serialize(tournament.ToGetTournamentDTO([captain, member], [team]));
        Assert.Contains("\"status\":\"Scheduled\"", json, StringComparison.Ordinal);
        Assert.Contains("\"participationMode\":\"Team\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void MatchSponsorSearchAndUserDtos_KeepCurrentJsonShape()
    {
        var user = CreateUser(5);
        var tournament = CreateTournament(ParticipationMode.Individual);
        var match = new Match
        {
            Id = Guid.NewGuid(),
            Tournament = tournament,
            TournamentId = tournament.Id,
            BracketType = BracketType.SingleElimination,
            Format = GameFormat.BestOf3,
            ParticipationMode = ParticipationMode.Individual,
            RoundNumber = 1,
            MatchNumber = 1,
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
        var searchResult = new DiscoverySearchResult(
            "user",
            user.Username!,
            "Player",
            user.Username,
            null,
            null);

        AssertJsonProperties(match.ToGetMatchDTO(),
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
            "tournamentId",
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
        AssertJsonProperties(GetSponsorDTO.From(sponsor), "id", "name", "sponsorTier", "logoUrl", "infoUrl", "description");
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

        var json = Serialize(GetSponsorDTO.From(sponsor));
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

    private static TournamentAggregate CreateTournament(ParticipationMode participationMode, int? teamSize = null)
    {
        return new TournamentAggregate(
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
        TournamentAggregate tournament,
        Team team,
        User captain,
        IReadOnlyCollection<User> rosterMembers,
        TournamentRegistrationStatus status)
    {
        var registration = new TournamentRegistration
        {
            Id = Guid.NewGuid(),
            Tournament = tournament,
            TournamentId = tournament.Id,
            Kind = TournamentRegistrationKind.Team,
            Status = status,
            RegisteredByUserId = captain.Id,
            RegisteredByUsernameAtRegistration = captain.Username ?? string.Empty,
            TeamId = team.Id,
            TeamNameAtRegistration = team.Name,
            TeamCaptainUserIdAtRegistration = team.CaptainUserId,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        registration.RosterMembers = rosterMembers.Select(member => new TournamentRegistrationRosterMember
        {
            Id = Guid.NewGuid(),
            TournamentRegistration = registration,
            TournamentRegistrationId = registration.Id,
            Tournament = tournament,
            TournamentId = tournament.Id,
            TeamId = team.Id,
            TeamNameAtRegistration = team.Name,
            UserId = member.Id,
            UsernameAtRegistration = member.Username ?? string.Empty,
            DisplayNameAtRegistration = member.DisplayName,
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
