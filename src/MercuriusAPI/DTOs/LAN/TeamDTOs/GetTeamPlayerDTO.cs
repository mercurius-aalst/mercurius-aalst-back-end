using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.DTOs.LAN.TeamDTOs;

public class GetTeamPlayerDTO
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Firstname { get; set; }
    public string Lastname { get; set; }

    public string? DiscordId { get; set; } = string.Empty;
    public string? SteamId { get; set; } = string.Empty;
    public string? RiotId { get; set; } = string.Empty;

    public string Email { get; set; }

    public GetTeamPlayerDTO()
    {

    }
    public GetTeamPlayerDTO(Player player)
    {
        Id = player.Id;
        Username = player.Username;
        Firstname = player.Firstname;
        Lastname = player.Lastname;
        DiscordId = player.DiscordId;
        SteamId = player.SteamId;
        RiotId = player.RiotId;
        Email = player.Email;
    }
}
