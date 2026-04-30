using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.DTOs.ParticipantDTOs;

public class GetParticipantUserDTO : GetParticipantDTO
{
    public string Username { get; set; } = string.Empty;
    public string Firstname { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DiscordId { get; set; } = string.Empty;
    public string? SteamId { get; set; } = string.Empty;
    public string? RiotId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    public GetParticipantUserDTO()
    {
    }

    public GetParticipantUserDTO(User user)
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
        Type = ParticipantType.Player;
    }
}
