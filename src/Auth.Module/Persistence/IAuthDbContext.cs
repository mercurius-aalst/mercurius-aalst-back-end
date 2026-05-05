using Auth.Module.Models;
using Microsoft.EntityFrameworkCore;

namespace Auth.Module.Persistence;

public interface IAuthDbContext
{
    DbSet<AuthUser> AuthUsers { get; }
    DbSet<ExternalIdentity> ExternalIdentities { get; }
    DbSet<Role> Roles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
