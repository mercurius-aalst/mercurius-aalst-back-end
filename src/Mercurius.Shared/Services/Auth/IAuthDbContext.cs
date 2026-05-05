using Mercurius.Shared.Models.Auth;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.Shared.Services.Auth;

public interface IAuthDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
