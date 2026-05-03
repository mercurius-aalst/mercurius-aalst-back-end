using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.DTOs.TeamDTOs;

public class GetTeamUserDTO
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string DisplayName { get; set; }
    public string Firstname { get; set; }
    public string Lastname { get; set; }

    public string? DiscordId { get; set; } = string.Empty;
    public string? SteamId { get; set; } = string.Empty;
    public string? RiotId { get; set; } = string.Empty;

    public string Email { get; set; }

    public GetTeamUserDTO()
    {

    }
    public GetTeamUserDTO(User user)
    {
        Id = user.Id;
        Username = user.Username;
        DisplayName = user.DisplayName;
        Firstname = user.Firstname;
        Lastname = user.Lastname;
        DiscordId = user.DiscordId;
        SteamId = user.SteamId;
        RiotId = user.RiotId;
        Email = user.Email;
    }
}

