using System.ComponentModel.DataAnnotations;

namespace Auth.Module.Models;

public class LoginRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Password { get; set; } = string.Empty;
}
