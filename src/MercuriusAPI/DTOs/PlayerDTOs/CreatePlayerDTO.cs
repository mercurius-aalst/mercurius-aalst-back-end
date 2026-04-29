using System.ComponentModel.DataAnnotations;

namespace Mercurius.LAN.API.DTOs.PlayerDTOs;

public class CreatePlayerDTO
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Username { get; set; }
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Firstname { get; set; }
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Lastname { get; set; }
    [StringLength(100)]
    public string? DiscordId { get; set; }
    [StringLength(100)]
    public string? SteamId { get; set; }
    [StringLength(100)]
    public string? RiotId { get; set; }
    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; set; }
}

