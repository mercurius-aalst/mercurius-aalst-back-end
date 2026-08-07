using Mercurius.Modules.Discovery.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Mercurius.Modules.Discovery.Infrastructure;

internal sealed class DiscoveryDbContextAdapter<TDbContext> : IDiscoveryDbContext
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;

    public DiscoveryDbContextAdapter(TDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public DbSet<SearchDocument> SearchDocuments => _dbContext.Set<SearchDocument>();
    public DbSet<SearchIndexRebuildJob> SearchIndexRebuildJobs => _dbContext.Set<SearchIndexRebuildJob>();

    public EntityEntry Entry(object entity) => _dbContext.Entry(entity);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
