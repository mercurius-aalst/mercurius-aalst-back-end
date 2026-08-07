using Mercurius.Modules.Discovery.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Mercurius.Modules.Discovery.Infrastructure;

internal interface IDiscoveryDbContext
{
    DbSet<SearchDocument> SearchDocuments { get; }
    DbSet<SearchIndexRebuildJob> SearchIndexRebuildJobs { get; }

    EntityEntry Entry(object entity);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
