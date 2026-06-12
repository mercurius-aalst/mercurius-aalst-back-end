using System.Text.Json;
using Mercurius.LAN.API.DTOs.Auth;
using Mercurius.LAN.API.DTOs.GameDTOs;
using Mercurius.LAN.API.DTOs.PlacementDTOs;
using Mercurius.LAN.API.DTOs.TeamDTOs;
using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.Tests;

public class PublicParticipantPrivacyDTOTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    [Fact]
    public void PublicUserDTO_ContainsOnlyPublicIdentityFields()
    {
        var json = Serialize(new PublicUserDTO(CreateUser(1)));

        Assert.Contains("\"id\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"username\":\"user1\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"displayName\":\"user1\"", json, StringComparison.OrdinalIgnoreCase);
        AssertPublicPlatformIdsArePresent(json, 1);
        AssertPrivateUserFieldsAreAbsent(json);
    }

    [Fact]
    public void GetGameDTO_UsesPrivacySafeRegistrationParticipants()
    {
        var game = CreateGame(ParticipationMode.Individual);
        var user = CreateUser(2);
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

        var json = Serialize(new GetGameDTO(game));

        Assert.Contains("\"registrations\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"users\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"teams\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"username\":\"user2\"", json, StringComparison.OrdinalIgnoreCase);
        AssertPublicPlatformIdsArePresent(json, 2);
        AssertPrivateUserFieldsAreAbsent(json);
    }

    [Fact]
    public void GetPlacementDTO_UsesPrivacySafeTeamMembers()
    {
        var placement = new Placement
        {
            Place = 1,
            Teams = [CreateTeam(3)]
        };

        var json = Serialize(new GetPlacementDTO(placement, ParticipationMode.Team));

        Assert.Contains("\"teams\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"members\":", json, StringComparison.OrdinalIgnoreCase);
        AssertPrivateUserFieldsAreAbsent(json);
    }

    [Fact]
    public void GetTeamDTO_OmitsInvitesAndUsesPrivacySafeMembers()
    {
        var team = CreateTeam(4);
        team.TeamInvites.Add(new TeamInvite
        {
            TeamId = team.Id,
            UserId = Guid.NewGuid(),
            Status = TeamInviteStatus.Pending
        });

        var json = Serialize(new GetTeamDTO(team));

        Assert.Contains("\"members\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"teamInvites\":", json, StringComparison.OrdinalIgnoreCase);
        AssertPrivateUserFieldsAreAbsent(json);
    }

    [Fact]
    public void GetGameDTO_ExposesPublicRegistrationRostersWithoutPrivateRegistrationMetadata()
    {
        var captain = CreateUser(5);
        var rosterMember = CreateUser(6);
        var team = new Team("Roster Team", captain) { Id = Guid.NewGuid() };
        team.Members.Add(CreateUser(99));
        var game = CreateGame(ParticipationMode.Team);
        var registration = new TournamentRegistration
        {
            Id = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            Kind = TournamentRegistrationKind.Team,
            Status = TournamentRegistrationStatus.Active,
            RegisteredByUser = captain,
            RegisteredByUserId = captain.Id,
            Team = team,
            TeamId = team.Id,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            RosterMembers =
            [
                new TournamentRegistrationRosterMember
                {
                    Id = Guid.NewGuid(),
                    Game = game,
                    GameId = game.Id,
                    Team = team,
                    TeamId = team.Id,
                    User = captain,
                    UserId = captain.Id,
                    IsCaptain = true,
                    ConfirmationStatus = RosterMemberConfirmationStatus.AutoConfirmed
                },
                new TournamentRegistrationRosterMember
                {
                    Id = Guid.NewGuid(),
                    Game = game,
                    GameId = game.Id,
                    Team = team,
                    TeamId = team.Id,
                    User = rosterMember,
                    UserId = rosterMember.Id,
                    ConfirmationStatus = RosterMemberConfirmationStatus.Confirmed
                }
            ]
        };
        game.TournamentRegistrations.Add(registration);
        var pendingUser = CreateUser(7);
        game.TournamentRegistrations.Add(new TournamentRegistration
        {
            Id = Guid.NewGuid(),
            Game = game,
            GameId = game.Id,
            Kind = TournamentRegistrationKind.Team,
            Status = TournamentRegistrationStatus.PendingConfirmation,
            RegisteredByUser = captain,
            RegisteredByUserId = captain.Id,
            Team = team,
            TeamId = team.Id,
            RosterMembers =
            [
                new TournamentRegistrationRosterMember
                {
                    Id = Guid.NewGuid(),
                    Game = game,
                    GameId = game.Id,
                    Team = team,
                    TeamId = team.Id,
                    User = pendingUser,
                    UserId = pendingUser.Id,
                    ConfirmationStatus = RosterMemberConfirmationStatus.Pending
                }
            ]
        });

        var json = Serialize(new GetGameDTO(game));

        Assert.Contains("\"registrations\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"rosterMembers\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"username\":\"user6\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"username\":\"user7\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"confirmationStatus\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"createdAtUtc\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"updatedAtUtc\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"confirmationInviteId\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"confirmationNotification", json, StringComparison.OrdinalIgnoreCase);
        AssertPrivateUserFieldsAreAbsent(json);
    }

    private static string Serialize<T>(T dto)
    {
        return JsonSerializer.Serialize(dto, WebJson);
    }

    private static void AssertPrivateUserFieldsAreAbsent(string json)
    {
        Assert.DoesNotContain("\"email\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"firstname\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"lastname\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"auth0UserId\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"emailVerified\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isDeleted\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"createdAtUtc\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"updatedAtUtc\":", json, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertPublicPlatformIdsArePresent(string json, int id)
    {
        Assert.Contains($"\"discordId\":\"discord-{id}\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"\"steamId\":\"steam-{id}\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"\"riotId\":\"riot-{id}\"", json, StringComparison.OrdinalIgnoreCase);
    }

    private static Game CreateGame(ParticipationMode participationMode)
    {
        return new Game(
            "Public Privacy Game",
            BracketType.SingleElimination,
            GameFormat.BestOf1,
            GameFormat.BestOf1,
            participationMode,
            participationMode == ParticipationMode.Team ? 2 : null);
    }

    private static User CreateUser(int id)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Auth0UserId = $"auth0|{id}",
            Username = $"user{id}",
            NormalizedUsername = $"user{id}",
            Firstname = $"First{id}",
            Lastname = $"Last{id}",
            Email = $"user{id}@example.com",
            DiscordId = $"discord-{id}",
            SteamId = $"steam-{id}",
            RiotId = $"riot-{id}"
        };
    }

    private static Team CreateTeam(int id)
    {
        var captain = CreateUser(id);
        var teammate = CreateUser(id + 10);
        var team = new Team($"Team {id}", captain)
        {
            Id = Guid.NewGuid()
        };
        team.Members.Add(teammate);
        return team;
    }
}
