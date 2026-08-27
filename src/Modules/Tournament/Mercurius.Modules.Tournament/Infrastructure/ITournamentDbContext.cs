using Mercurius.Modules.Tournament.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Mercurius.Modules.Tournament.Infrastructure;

internal interface ITournamentDbContext
{
    DbSet<TournamentAggregate> Tournaments { get; }
    DbSet<Match> Matches { get; }
    DbSet<Placement> Placements { get; }
    DbSet<TournamentRegistration> TournamentRegistrations { get; }
    DbSet<TournamentRegistrationRosterMember> TournamentRegistrationRosterMembers { get; }
    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
