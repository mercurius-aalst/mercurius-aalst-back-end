using System.ComponentModel.DataAnnotations;

namespace Mercurius.Shared.DTOs.Auth;

public class CreateUserProfileRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Username { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Password { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Firstname { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Lastname { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; set; }

    [StringLength(100)]
    public string? DiscordId { get; set; }

    [StringLength(100)]
    public string? SteamId { get; set; }

    [StringLength(100)]
    public string? RiotId { get; set; }
}
