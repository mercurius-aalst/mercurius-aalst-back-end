namespace Mercurius.LAN.API.Models;

public class User
{
    public Guid Id { get; set; }
    public string Auth0Subject { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Firstname { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DiscordId { get; set; } = string.Empty;
    public string? SteamId { get; set; } = string.Empty;
    public string? RiotId { get; set; } = string.Empty;

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
