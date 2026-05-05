namespace Auth.Module.Models;

public class RevokeTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
