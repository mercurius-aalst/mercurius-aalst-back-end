using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.DTOs.Auth;

public class GetUserDTO
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Firstname { get; set; }
    public string Lastname { get; set; }
    public string Email { get; set; }
    public string? DiscordId { get; set; }
    public string? SteamId { get; set; }
    public string? RiotId { get; set; }
    public string DisplayName { get; set; }
    public IEnumerable<string> Roles { get; set; }

    public GetUserDTO(User user)
    {
        Id = user.Id;
        Username = user.Username;
        Firstname = user.Firstname;
        Lastname = user.Lastname;
        Email = user.Email;
        DiscordId = user.DiscordId;
        SteamId = user.SteamId;
        RiotId = user.RiotId;
        DisplayName = user.DisplayName;
        Roles = user.Roles.Select(r => r.Name);
    }
}
