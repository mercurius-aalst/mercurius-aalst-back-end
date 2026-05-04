namespace Mercurius.LAN.API.Models.Auth;

public enum ExternalAuthProvider
{
    Google = 1,
    Facebook = 2,
}

public class ExternalIdentity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public ExternalAuthProvider Provider { get; set; }
    public string ProviderSubject { get; set; } = string.Empty;

    public string? EmailAtLinkTime { get; set; }
    public bool EmailVerifiedAtLinkTime { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAtUtc { get; set; }
}
