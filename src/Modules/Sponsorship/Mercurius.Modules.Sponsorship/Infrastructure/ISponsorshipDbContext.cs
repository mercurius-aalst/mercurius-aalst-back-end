using Mercurius.Modules.Sponsorship.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Mercurius.Modules.Sponsorship.Infrastructure;

internal interface ISponsorshipDbContext
{
    DbSet<Sponsor> Sponsors { get; }
    DbSet<TournamentSponsorPlacement> TournamentSponsorPlacements { get; }
    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
