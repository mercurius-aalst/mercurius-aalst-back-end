namespace MercuriusAPI.DTOs.LAN.PlayerDTOs;

public class CreatePlayerDTO
{
    public string Username { get; set; }
    public string Firstname { get; set; }
    public string Lastname { get; set; }
    public string? DiscordId { get; set; }
    public string? SteamId { get; set; }
    public string? RiotId { get; set; }
    public string Email { get; set; }
}
