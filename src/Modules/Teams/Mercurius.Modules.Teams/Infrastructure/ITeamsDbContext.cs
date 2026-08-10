using Mercurius.Modules.Teams.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Mercurius.Modules.Teams.Infrastructure;

internal interface ITeamsDbContext
{
    DbSet<Team> Teams { get; }
    DatabaseFacade Database { get; }

    DbSet<TEntity> Set<TEntity>()
        where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
