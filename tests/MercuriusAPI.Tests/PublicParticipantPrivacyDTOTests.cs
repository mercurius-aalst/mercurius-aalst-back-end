using System.Text.Json;
using Mercurius.LAN.API.DTOs.GameDTOs;
using Mercurius.LAN.API.DTOs.PlacementDTOs;
using Mercurius.LAN.API.DTOs.TeamDTOs;
using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.Tests;

public class PublicParticipantPrivacyDTOTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    [Fact]
    public void GetPublicGameDTO_AnonymousResponse_OmitsPrivateUserFields()
    {
        var game = CreateIndividualGame();
        var user = CreateUser(1);
        game.RegisterUser(user);
        game.Placements.Add(new Placement
        {
            Place = 1,
            Users = [user]
        });

        var dto = new GetPublicGameDTO(game, includePlatformIds: false);
        var json = JsonSerializer.Serialize(dto, WebJson);

        Assert.DoesNotContain("\"email\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"firstname\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"lastname\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"auth0UserId\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"isDeleted\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"createdAtUtc\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"updatedAtUtc\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"discordId\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"steamId\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"riotId\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"displayName\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"username\":", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetPublicPlacementDTO_AnonymousTeamPlacement_OmitsPrivateMemberFields()
    {
        var team = CreateTeam(1);
        var placement = new Placement
        {
            Place = 1,
            Teams = [team]
        };

        var dto = new GetPublicPlacementDTO(placement, ParticipationMode.Team, includePlatformIds: false);
        var json = JsonSerializer.Serialize(dto, WebJson);

        Assert.DoesNotContain("\"email\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"firstname\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"lastname\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"discordId\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"steamId\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"riotId\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"members\":", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetPublicTeamDTO_AnonymousResponse_OmitsInvitesAndPrivateMemberFields()
    {
        var team = CreateTeam(2);
        team.TeamInvites.Add(new TeamInvite
        {
            TeamId = team.Id,
            UserId = Guid.NewGuid(),
            Status = TeamInviteStatus.Pending
        });

        var dto = new GetPublicTeamDTO(team, includePlatformIds: false);
        var json = JsonSerializer.Serialize(dto, WebJson);

        Assert.DoesNotContain("\"teamInvites\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"email\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"firstname\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"lastname\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"discordId\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"steamId\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"riotId\":", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"members\":", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetPublicGameDTO_AuthenticatedPublicResponse_IncludesPlatformIdsWhenEnabled()
    {
        var game = CreateIndividualGame();
        var user = CreateUser(3);
        game.RegisterUser(user);

        var dto = new GetPublicGameDTO(game, includePlatformIds: true);
        var json = JsonSerializer.Serialize(dto, WebJson);

        Assert.Contains("\"discordId\":\"discord-3\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"steamId\":\"steam-3\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"riotId\":\"riot-3\"", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetGameDTO_AdminResponse_StillIncludesPrivateUserFields()
    {
        var game = CreateIndividualGame();
        var user = CreateUser(4);
        game.RegisterUser(user);

        var dto = new GetGameDTO(game);
        var json = JsonSerializer.Serialize(dto, WebJson);

        Assert.Contains("\"email\":\"user4@example.com\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"firstname\":\"First4\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"lastname\":\"Last4\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"discordId\":\"discord-4\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"steamId\":\"steam-4\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"riotId\":\"riot-4\"", json, StringComparison.OrdinalIgnoreCase);
    }

    private static Game CreateIndividualGame()
    {
        return new Game(
            "Public Privacy Game",
            BracketType.SingleElimination,
            GameFormat.BestOf1,
            GameFormat.BestOf1,
            ParticipationMode.Individual,
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
