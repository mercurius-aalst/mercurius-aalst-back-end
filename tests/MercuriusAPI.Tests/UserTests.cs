using Mercurius.LAN.API.DTOs.Auth;
using Mercurius.LAN.API.Models;
using Mercurius.LAN.API.Models.Auth;

namespace Mercurius.LAN.API.Tests;

public class UserTests
{
    [Fact]
    public void UpdateProfile_UpdatesCanonicalIdentityFields()
    {
        var user = new User
        {
            Username = "user",
            Firstname = "OldFirst",
            Lastname = "OldLast",
            Email = "old@test.com"
        };

        user.UpdateProfile("NewFirst", "NewLast", "new@test.com", "discord", "steam", "riot");

        Assert.Equal("NewFirst", user.Firstname);
        Assert.Equal("NewLast", user.Lastname);
        Assert.Equal("new@test.com", user.Email);
        Assert.Equal("discord", user.DiscordId);
        Assert.Equal("steam", user.SteamId);
        Assert.Equal("riot", user.RiotId);
        Assert.Equal("NewFirst NewLast", user.DisplayName);
        Assert.Equal("user", user.Username);
    }

    [Fact]
    public void GetUserDTO_MapsProfileFieldsAndDisplayName()
    {
        var user = new User
        {
            Id = 7,
            Username = "playerone",
            Firstname = "Player",
            Lastname = "One",
            Email = "playerone@test.com",
            DiscordId = "discord-1",
            SteamId = "steam-1",
            RiotId = "riot-1",
            Roles = [new Role { Name = "admin" }, new Role { Name = "captain" }]
        };

        var dto = new GetUserDTO(user);

        Assert.Equal(7, dto.Id);
        Assert.Equal("playerone", dto.Username);
        Assert.Equal("Player", dto.Firstname);
        Assert.Equal("One", dto.Lastname);
        Assert.Equal("playerone@test.com", dto.Email);
        Assert.Equal("discord-1", dto.DiscordId);
        Assert.Equal("steam-1", dto.SteamId);
        Assert.Equal("riot-1", dto.RiotId);
        Assert.Equal("Player One", dto.DisplayName);
        Assert.Equal(["admin", "captain"], dto.Roles);
    }

    [Fact]
    public void DisplayName_FallsBackToUsername_WhenNameIsMissing()
    {
        var user = new User
        {
            Username = "fallback-user",
            Firstname = "",
            Lastname = " "
        };

        Assert.Equal("fallback-user", user.DisplayName);
    }
}
