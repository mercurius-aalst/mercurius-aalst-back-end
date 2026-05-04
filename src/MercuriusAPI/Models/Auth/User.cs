using Mercurius.LAN.API.Models.Auth;

namespace Mercurius.LAN.API.Models;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Firstname { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DiscordId { get; set; } = string.Empty;
    public string? SteamId { get; set; } = string.Empty;
    public string? RiotId { get; set; } = string.Empty;
    public byte[]? PasswordHash { get; set; }
    public byte[]? Salt { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Role> Roles { get; set; } = new List<Role>();
    public ICollection<ExternalIdentity> ExternalIdentities { get; set; } = new List<ExternalIdentity>();

    public string DisplayName
    {
        get
        {
            var fullName = $"{Firstname} {Lastname}".Trim();
            return string.IsNullOrWhiteSpace(fullName) ? Username : fullName;
        }
    }

    public void UpdateProfile(string firstname, string lastname, string email, string? discordId, string? steamId, string? riotId)
    {
        Firstname = firstname;
        Lastname = lastname;
        Email = email;
        DiscordId = discordId;
        SteamId = steamId;
        RiotId = riotId;
    }
}
