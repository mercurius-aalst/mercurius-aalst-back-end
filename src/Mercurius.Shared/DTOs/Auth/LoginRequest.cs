using System.ComponentModel.DataAnnotations;

namespace Mercurius.Shared.DTOs.Auth;

public class LoginRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Username { get; set; }
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Password { get; set; }
}
