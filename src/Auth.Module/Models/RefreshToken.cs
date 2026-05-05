namespace Auth.Module.Models;

public class RefreshToken
{
    public Guid Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime Expires { get; set; }
    public Guid UserId { get; set; }
    public AuthUser User { get; set; } = null!;
}
