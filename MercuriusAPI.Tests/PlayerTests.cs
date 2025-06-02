using MercuriusAPI.Models.LAN;
using Xunit;

namespace MercuriusAPI.Tests;
public class PlayerTests
{
    [Fact]
    public void Update_UpdatesPlayerProperties()
    {
        // Arrange
        var player = new Player("user", "OldFirst", "OldLast", "email@test.com", null, null, null);

        // Act
        player.Update("NewFirst", "NewLast", "NewUser", "discord", "steam", "riot");

        // Assert
        Assert.Equal("NewFirst", player.Firstname);
        Assert.Equal("NewLast", player.Lastname);
        Assert.Equal("email@test.com", player.Email);
        Assert.Equal("discord", player.DiscordId);
        Assert.Equal("steam", player.SteamId);
        Assert.Equal("riot", player.RiotId);
    }

    [Fact]
    public void Update_EmptyIds_UpdatesProperties()
    {
        var player = new Player("user", "OldFirst", "OldLast", "email@test.com", null, null, null);

        player.Update(player.Firstname, player.Lastname,player.Username, string.Empty, string.Empty, string.Empty);

        Assert.Equal(string.Empty, player.DiscordId);
        Assert.Equal(string.Empty, player.SteamId);
        Assert.Equal(string.Empty, player.RiotId);
        Assert.Equal("user", player.Username);
        Assert.Equal("OldFirst", player.Firstname);
        Assert.Equal("OldLast", player.Lastname);
        Assert.Equal("email@test.com", player.Email);
    }
}
