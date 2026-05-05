namespace Auth.Module.Models;

public class AuthUser
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public byte[]? PasswordHash { get; set; }
    public byte[]? Salt { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<ExternalIdentity> ExternalIdentities { get; set; } = [];
    public ICollection<Role> Roles { get; set; } = [];
}
