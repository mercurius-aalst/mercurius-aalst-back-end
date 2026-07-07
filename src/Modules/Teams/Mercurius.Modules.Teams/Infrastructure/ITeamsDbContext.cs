using Mercurius.Modules.Identity.Domain;
using Mercurius.Modules.Teams.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Mercurius.Modules.Teams.Infrastructure;

public interface ITeamsDbContext
{
    DbSet<Team> Teams { get; }
    DbSet<User> Users { get; }
    ChangeTracker ChangeTracker { get; }
    DatabaseFacade Database { get; }

    DbSet<TEntity> Set<TEntity>()
        where TEntity : class;

    EntityEntry<TEntity> Entry<TEntity>(TEntity entity)
        where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
