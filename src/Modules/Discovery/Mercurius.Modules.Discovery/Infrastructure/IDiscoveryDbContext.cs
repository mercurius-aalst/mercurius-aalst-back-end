using Mercurius.Modules.Discovery.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;

namespace Mercurius.Modules.Discovery.Infrastructure;

internal interface IDiscoveryDbContext
{
    DbSet<SearchDocument> SearchDocuments { get; }
    DbSet<SearchIndexRebuildJob> SearchIndexRebuildJobs { get; }
    DbSet<SearchIndexRebuildDocument> SearchIndexRebuildDocuments { get; }

    bool IsRelational { get; }

    EntityEntry Entry(object entity);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task<int> ExecuteSqlInterpolatedAsync(FormattableString sql, CancellationToken cancellationToken = default);
}
