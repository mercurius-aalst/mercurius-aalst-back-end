using Mercurius.Modules.Competition.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Mercurius.Modules.Competition.Infrastructure;

internal sealed class CompetitionDbContextAdapter<TDbContext> : ICompetitionDbContext
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;

    public CompetitionDbContextAdapter(TDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public DbSet<Game> Games => _dbContext.Set<Game>();
    public DbSet<Match> Matches => _dbContext.Set<Match>();
    public DbSet<Placement> Placements => _dbContext.Set<Placement>();
    public DbSet<TournamentRegistration> TournamentRegistrations => _dbContext.Set<TournamentRegistration>();
    public DbSet<TournamentRegistrationRosterMember> TournamentRegistrationRosterMembers =>
        _dbContext.Set<TournamentRegistrationRosterMember>();
    public DatabaseFacade Database => _dbContext.Database;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
