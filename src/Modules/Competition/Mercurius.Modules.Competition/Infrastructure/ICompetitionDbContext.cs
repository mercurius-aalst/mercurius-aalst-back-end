using Mercurius.Modules.Competition.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Mercurius.Modules.Competition.Infrastructure;

internal interface ICompetitionDbContext
{
    DbSet<Game> Games { get; }
    DbSet<Match> Matches { get; }
    DbSet<Placement> Placements { get; }
    DbSet<TournamentRegistration> TournamentRegistrations { get; }
    DbSet<TournamentRegistrationRosterMember> TournamentRegistrationRosterMembers { get; }
    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
