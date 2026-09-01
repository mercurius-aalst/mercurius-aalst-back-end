using Mercurius.Modules.Tournament.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Mercurius.Modules.Tournament.Infrastructure;

internal sealed class TournamentDbContextAdapter<TDbContext> : ITournamentDbContext
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;

    public TournamentDbContextAdapter(TDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public DbSet<TournamentAggregate> Tournaments => _dbContext.Set<TournamentAggregate>();
    public DbSet<Match> Matches => _dbContext.Set<Match>();
    public DbSet<Placement> Placements => _dbContext.Set<Placement>();
    public DbSet<MatchResolutionNotification> MatchResolutionNotifications =>
        _dbContext.Set<MatchResolutionNotification>();
    public DbSet<TournamentRegistration> TournamentRegistrations => _dbContext.Set<TournamentRegistration>();
    public DbSet<TournamentRegistrationRosterMember> TournamentRegistrationRosterMembers =>
        _dbContext.Set<TournamentRegistrationRosterMember>();
    public DatabaseFacade Database => _dbContext.Database;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
