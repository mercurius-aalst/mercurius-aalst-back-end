using Microsoft.EntityFrameworkCore;

namespace Platform.Eventing.Persistence;

public interface IModuleEventDbContext
{
    DbSet<OutboxMessage> OutboxMessages { get; }
    DbSet<InboxMessage> InboxMessages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
