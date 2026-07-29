using Mercurius.Modules.Identity.Domain;
using Mercurius.Modules.Teams.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Mercurius.Modules.Teams.Infrastructure;

internal sealed class TeamsDbContextAdapter<TDbContext> : ITeamsDbContext
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;

    public TeamsDbContextAdapter(TDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public DbSet<Team> Teams => _dbContext.Set<Team>();
    public DbSet<User> Users => _dbContext.Set<User>();
    public ChangeTracker ChangeTracker => _dbContext.ChangeTracker;
    public DatabaseFacade Database => _dbContext.Database;

    public DbSet<TEntity> Set<TEntity>()
        where TEntity : class
    {
        return _dbContext.Set<TEntity>();
    }

    public EntityEntry<TEntity> Entry<TEntity>(TEntity entity)
        where TEntity : class
    {
        return _dbContext.Entry(entity);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
