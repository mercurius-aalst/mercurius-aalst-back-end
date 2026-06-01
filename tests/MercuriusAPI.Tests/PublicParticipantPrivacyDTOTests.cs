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
    public void GetGameDTO_UsesPrivacySafeParticipants()
    {
        var game = CreateGame(ParticipationMode.Individual);
        game.RegisterUser(CreateUser(2));

        var json = Serialize(new GetGameDTO(game));

        Assert.Contains("\"users\":", json, StringComparison.OrdinalIgnoreCase);
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
            "https://example.com/register");
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
