using System.ComponentModel.DataAnnotations;

namespace Mercurius.LAN.API.DTOs.Auth;

public class UpdateUserProfileRequest
{
    [Required]
    [StringLength(32, MinimumLength = 3)]
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
}
