namespace Auth.Module.Models;

public class ExternalIdentity
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ProviderSubject { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool EmailVerified { get; set; }
    public DateTime LinkedAtUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
    public Guid UserId { get; set; }
    public AuthUser User { get; set; } = null!;
}
