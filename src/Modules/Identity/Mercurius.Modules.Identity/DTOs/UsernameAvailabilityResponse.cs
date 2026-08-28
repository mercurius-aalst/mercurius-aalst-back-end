namespace Mercurius.Modules.Identity.DTOs;

internal sealed class UsernameAvailabilityResponse
{
    public string Username { get; set; } = string.Empty;
    public string? NormalizedUsername { get; set; }
    public bool IsAvailable { get; set; }
    public string? Reason { get; set; }
}
