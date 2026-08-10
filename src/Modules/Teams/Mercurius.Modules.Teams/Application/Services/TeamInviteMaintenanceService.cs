using System.Data;
using Mercurius.Modules.Teams.Contracts;
using Mercurius.Modules.Teams.Domain;
using Mercurius.Modules.Teams.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace Mercurius.Modules.Teams.Services;

internal sealed class TeamInviteMaintenanceService
{
    private const long MaintenanceLockKey = 0x5445414D494E5654;
    private readonly ITeamsDbContext _dbContext;
    private readonly ITeamEventPublisher _teamEventPublisher;
    private readonly TeamInviteMaintenanceOptions _options;
    private DbSet<TeamInvite> TeamInvites => _dbContext.Set<TeamInvite>();

    public TeamInviteMaintenanceService(
        ITeamsDbContext dbContext,
        ITeamEventPublisher teamEventPublisher,
        IOptions<TeamInviteMaintenanceOptions> options)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _teamEventPublisher = teamEventPublisher ?? throw new ArgumentNullException(nameof(teamEventPublisher));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<int> RunBatchAsync(CancellationToken cancellationToken = default)
    {
        IDbContextTransaction? transaction = null;
        var expiredEvents = new List<ExpiredInviteEvent>();
        var deletedCount = 0;

        try
        {
            if (_dbContext.Database.IsRelational())
            {
                transaction = await _dbContext.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable,
                    cancellationToken);

                if (!await TryAcquireMaintenanceLockAsync(cancellationToken))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return 0;
                }
            }

            var now = DateTime.UtcNow;
            var expiredInvites = await TeamInvites
                .Where(invite =>
                    invite.Status == TeamInviteStatus.Pending &&
                    invite.ExpiresAt <= now)
                .OrderBy(invite => invite.ExpiresAt)
                .ThenBy(invite => invite.Id)
                .Take(_options.MaintenanceBatchSize)
                .ToListAsync(cancellationToken);

            foreach (var invite in expiredInvites)
            {
                invite.Expire();
                expiredEvents.Add(new ExpiredInviteEvent(invite.TeamId, invite.Id, invite.UserId));
            }

            if (expiredInvites.Count > 0)
                await _dbContext.SaveChangesAsync(cancellationToken);

            var retentionCutoff = now.AddDays(-_options.RetentionDays);
            var cleanupIds = await GetTerminalInviteCleanupCandidateIdsAsync(
                retentionCutoff,
                cancellationToken);

            if (cleanupIds.Count > 0)
            {
                var cleanupQuery = TeamInvites.Where(invite => cleanupIds.Contains(invite.Id));
                if (_dbContext.Database.IsRelational())
                {
                    deletedCount = await cleanupQuery.ExecuteDeleteAsync(cancellationToken);
                }
                else
                {
                    var cleanupInvites = await cleanupQuery.ToListAsync(cancellationToken);
                    TeamInvites.RemoveRange(cleanupInvites);
                    deletedCount = await _dbContext.SaveChangesAsync(cancellationToken);
                }
            }

            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            if (transaction is not null)
                await transaction.RollbackAsync(CancellationToken.None);

            throw;
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }

        await PublishExpiredInviteEventsAsync(expiredEvents, cancellationToken);
        return expiredEvents.Count + deletedCount;
    }

    private async Task<List<Guid>> GetTerminalInviteCleanupCandidateIdsAsync(
        DateTime cutoff,
        CancellationToken cancellationToken)
    {
        var respondedInvites = await TeamInvites
            .AsNoTracking()
            .Where(invite =>
                (invite.Status == TeamInviteStatus.Accepted || invite.Status == TeamInviteStatus.Declined) &&
                invite.RespondedAt.HasValue &&
                invite.RespondedAt.Value < cutoff)
            .OrderBy(invite => invite.RespondedAt)
            .ThenBy(invite => invite.Id)
            .Take(_options.MaintenanceBatchSize)
            .Select(invite => new TerminalInviteCandidate
            {
                Id = invite.Id,
                TerminalAt = invite.RespondedAt!.Value
            })
            .ToListAsync(cancellationToken);

        var cancelledInvites = await TeamInvites
            .AsNoTracking()
            .Where(invite =>
                invite.Status == TeamInviteStatus.Cancelled &&
                invite.CancelledAt.HasValue &&
                invite.CancelledAt.Value < cutoff)
            .OrderBy(invite => invite.CancelledAt)
            .ThenBy(invite => invite.Id)
            .Take(_options.MaintenanceBatchSize)
            .Select(invite => new TerminalInviteCandidate
            {
                Id = invite.Id,
                TerminalAt = invite.CancelledAt!.Value
            })
            .ToListAsync(cancellationToken);

        var expiredInvites = await TeamInvites
            .AsNoTracking()
            .Where(invite =>
                invite.Status == TeamInviteStatus.Expired &&
                invite.ExpiredAt.HasValue &&
                invite.ExpiredAt.Value < cutoff)
            .OrderBy(invite => invite.ExpiredAt)
            .ThenBy(invite => invite.Id)
            .Take(_options.MaintenanceBatchSize)
            .Select(invite => new TerminalInviteCandidate
            {
                Id = invite.Id,
                TerminalAt = invite.ExpiredAt!.Value
            })
            .ToListAsync(cancellationToken);

        return respondedInvites
            .Concat(cancelledInvites)
            .Concat(expiredInvites)
            .OrderBy(candidate => candidate.TerminalAt)
            .ThenBy(candidate => candidate.Id)
            .Take(_options.MaintenanceBatchSize)
            .Select(candidate => candidate.Id)
            .ToList();
    }

    private async Task<bool> TryAcquireMaintenanceLockAsync(CancellationToken cancellationToken)
    {
        if (!string.Equals(
                _dbContext.Database.ProviderName,
                "Npgsql.EntityFrameworkCore.PostgreSQL",
                StringComparison.Ordinal))
        {
            return true;
        }

        return await _dbContext.Database
            .SqlQueryRaw<bool>($"SELECT pg_try_advisory_xact_lock({MaintenanceLockKey}) AS \"Value\"")
            .SingleAsync(cancellationToken);
    }

    private async Task PublishExpiredInviteEventsAsync(
        IReadOnlyCollection<ExpiredInviteEvent> expiredEvents,
        CancellationToken cancellationToken)
    {
        if (expiredEvents.Count == 0)
            return;

        await Parallel.ForEachAsync(
            expiredEvents,
            new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = Math.Min(_options.MaintenanceEventConcurrency, expiredEvents.Count)
            },
            async (expiredEvent, eventCancellationToken) =>
            {
                await _teamEventPublisher.InviteChangedAsync(
                    expiredEvent.TeamId,
                    expiredEvent.InviteId,
                    expiredEvent.UserId,
                    nameof(TeamInviteStatus.Expired),
                    eventCancellationToken);
            });
    }

    private sealed class TerminalInviteCandidate
    {
        public Guid Id { get; init; }
        public DateTime TerminalAt { get; init; }
    }

    private sealed record ExpiredInviteEvent(Guid TeamId, Guid InviteId, Guid UserId);
}
