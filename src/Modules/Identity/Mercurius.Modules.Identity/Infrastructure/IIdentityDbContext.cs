using Mercurius.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.Modules.Identity.Infrastructure;

public interface IIdentityDbContext
{
    DbSet<User> Users { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
