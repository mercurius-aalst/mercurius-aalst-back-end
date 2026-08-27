using Mercurius.Modules.Sponsorship.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Mercurius.Modules.Sponsorship.Infrastructure;

internal sealed class SponsorshipDbContextAdapter<TDbContext> : ISponsorshipDbContext
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;

    public SponsorshipDbContextAdapter(TDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public DbSet<Sponsor> Sponsors => _dbContext.Set<Sponsor>();
    public DbSet<TournamentSponsorPlacement> TournamentSponsorPlacements => _dbContext.Set<TournamentSponsorPlacement>();
    public DatabaseFacade Database => _dbContext.Database;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
