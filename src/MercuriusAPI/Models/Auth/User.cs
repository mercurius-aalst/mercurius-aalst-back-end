using Mercurius.LAN.API.Models.Auth;

namespace Mercurius.LAN.API.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public byte[] PasswordHash { get; set; }
    public byte[] Salt { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<Role> Roles { get; set; } = new List<Role>();
}
