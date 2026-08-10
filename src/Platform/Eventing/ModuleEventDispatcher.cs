using System.Collections;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Platform.Eventing.Persistence;

namespace Platform.Eventing;

internal sealed class ModuleEventDispatcher : IModuleEventDispatcher
{
    internal const string PostgreSqlClaimSql =
        """
        WITH candidate AS (
            SELECT id
            FROM platform.outbox_messages
            WHERE processed_at_utc IS NULL
              AND dead_lettered_at_utc IS NULL
              AND (next_attempt_at_utc IS NULL OR next_attempt_at_utc <= {0})
              AND (lease_expires_at_utc IS NULL OR lease_expires_at_utc <= {0})
            ORDER BY occurred_at_utc, id
            FOR UPDATE SKIP LOCKED
            LIMIT 1
        )
        UPDATE platform.outbox_messages AS message
        SET lease_id = {1},
            lease_expires_at_utc = {2}
        FROM candidate
        WHERE message.id = candidate.id
        RETURNING message.id AS "Value"
        """;

    private const int LastErrorMaxLength = 4000;
    private const string NpgsqlProviderName = "Npgsql.EntityFrameworkCore.PostgreSQL";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IModuleEventDbContext _dbContext;
    private readonly DbContext _efDbContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ModuleEventClaimGate _claimGate;
    private readonly ModuleEventingOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ModuleEventDispatcher> _logger;

    public ModuleEventDispatcher(
        IModuleEventDbContext dbContext,
        IServiceProvider serviceProvider,
        IServiceScopeFactory scopeFactory,
        ModuleEventClaimGate claimGate,
        IOptions<ModuleEventingOptions> options,
        TimeProvider timeProvider,
        ILogger<ModuleEventDispatcher> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _efDbContext = dbContext as DbContext
            ?? throw new ArgumentException("The module event DbContext must derive from DbContext.", nameof(dbContext));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _claimGate = claimGate ?? throw new ArgumentNullException(nameof(claimGate));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<int> DispatchPendingAsync(int batchSize = 50, CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than zero.");

        var processedCount = 0;
        for (var attempt = 0; attempt < batchSize; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var leaseId = Guid.NewGuid();
            var messageId = await ClaimNextMessageAsync(leaseId, cancellationToken);
            if (!messageId.HasValue)
                break;

            if (await DispatchClaimedMessageAsync(messageId.Value, leaseId, cancellationToken))
                processedCount++;
        }

        return processedCount;
    }

    public async Task<int> CleanupTerminalAsync(CancellationToken cancellationToken = default)
    {
        var now = GetUtcNow();
        var successfulCutoff = now - _options.SuccessfulRetention;
        var deadLetterCutoff = now - _options.DeadLetterRetention;

        var successful = await _dbContext.OutboxMessages
            .AsNoTracking()
            .Where(message => message.ProcessedAtUtc.HasValue && message.ProcessedAtUtc.Value <= successfulCutoff)
            .OrderBy(message => message.ProcessedAtUtc)
            .ThenBy(message => message.Id)
            .Take(_options.CleanupBatchSize)
            .Select(message => new TerminalMessageCandidate(message.Id, message.ProcessedAtUtc!.Value))
            .ToListAsync(cancellationToken);

        var deadLettered = await _dbContext.OutboxMessages
            .AsNoTracking()
            .Where(message => message.DeadLetteredAtUtc.HasValue && message.DeadLetteredAtUtc.Value <= deadLetterCutoff)
            .OrderBy(message => message.DeadLetteredAtUtc)
            .ThenBy(message => message.Id)
            .Take(_options.CleanupBatchSize)
            .Select(message => new TerminalMessageCandidate(message.Id, message.DeadLetteredAtUtc!.Value))
            .ToListAsync(cancellationToken);

        var messageIds = successful
            .Concat(deadLettered)
            .OrderBy(message => message.TerminalAtUtc)
            .ThenBy(message => message.Id)
            .Take(_options.CleanupBatchSize)
            .Select(message => message.Id)
            .ToArray();

        if (messageIds.Length == 0)
            return 0;

        IDbContextTransaction? transaction = null;
        try
        {
            if (_efDbContext.Database.IsRelational())
            {
                transaction = await _efDbContext.Database.BeginTransactionAsync(cancellationToken);
                await _dbContext.InboxMessages
                    .Where(message => messageIds.Contains(message.MessageId))
                    .ExecuteDeleteAsync(cancellationToken);
                var deleted = await _dbContext.OutboxMessages
                    .Where(message => messageIds.Contains(message.Id))
                    .ExecuteDeleteAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return deleted;
            }

            var inboxMessages = await _dbContext.InboxMessages
                .Where(message => messageIds.Contains(message.MessageId))
                .ToListAsync(cancellationToken);
            var outboxMessages = await _dbContext.OutboxMessages
                .Where(message => messageIds.Contains(message.Id))
                .ToListAsync(cancellationToken);
            _dbContext.InboxMessages.RemoveRange(inboxMessages);
            _dbContext.OutboxMessages.RemoveRange(outboxMessages);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return outboxMessages.Count;
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
    }

    private async Task<Guid?> ClaimNextMessageAsync(Guid leaseId, CancellationToken cancellationToken)
    {
        var now = GetUtcNow();
        var leaseExpiresAtUtc = now + _options.LeaseDuration;
        _efDbContext.ChangeTracker.Clear();

        if (string.Equals(_efDbContext.Database.ProviderName, NpgsqlProviderName, StringComparison.Ordinal))
        {
            var claimedIds = await _efDbContext.Database
                .SqlQueryRaw<Guid>(PostgreSqlClaimSql, now, leaseId, leaseExpiresAtUtc)
                .ToListAsync(cancellationToken);
            return claimedIds.Count == 0 ? null : claimedIds[0];
        }

        await _claimGate.EnterAsync(cancellationToken);
        try
        {
            var message = await _dbContext.OutboxMessages
                .Where(candidate =>
                    candidate.ProcessedAtUtc == null &&
                    candidate.DeadLetteredAtUtc == null &&
                    (candidate.NextAttemptAtUtc == null || candidate.NextAttemptAtUtc <= now) &&
                    (candidate.LeaseExpiresAtUtc == null || candidate.LeaseExpiresAtUtc <= now))
                .OrderBy(candidate => candidate.OccurredAtUtc)
                .ThenBy(candidate => candidate.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (message is null)
                return null;

            message.LeaseId = leaseId;
            message.LeaseExpiresAtUtc = leaseExpiresAtUtc;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return message.Id;
        }
        finally
        {
            _claimGate.Exit();
        }
    }

    private async Task<bool> DispatchClaimedMessageAsync(
        Guid messageId,
        Guid leaseId,
        CancellationToken cancellationToken)
    {
        _efDbContext.ChangeTracker.Clear();
        var message = await _dbContext.OutboxMessages
            .SingleOrDefaultAsync(candidate =>
                    candidate.Id == messageId &&
                    candidate.LeaseId == leaseId &&
                    candidate.ProcessedAtUtc == null &&
                    candidate.DeadLetteredAtUtc == null,
                cancellationToken);
        if (message is null)
            return false;

        using var stopHeartbeat = new CancellationTokenSource();
        using var ownershipLost = new CancellationTokenSource();
        var heartbeat = RunLeaseHeartbeatAsync(messageId, leaseId, stopHeartbeat.Token, ownershipLost);
        using var handlerCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            ownershipLost.Token);

        try
        {
            await DispatchMessageHandlersAsync(message, handlerCancellation.Token);
            await StopHeartbeatAsync(stopHeartbeat, heartbeat);

            return !ownershipLost.IsCancellationRequested &&
                   await MarkProcessedAsync(messageId, leaseId, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await StopHeartbeatAsync(stopHeartbeat, heartbeat);
            _efDbContext.ChangeTracker.Clear();
            await ReleaseLeaseAsync(messageId, leaseId, CancellationToken.None);
            throw;
        }
        catch (OperationCanceledException) when (ownershipLost.IsCancellationRequested)
        {
            await StopHeartbeatAsync(stopHeartbeat, heartbeat);
            _efDbContext.ChangeTracker.Clear();
            return false;
        }
        catch (Exception exception)
        {
            await StopHeartbeatAsync(stopHeartbeat, heartbeat);
            _efDbContext.ChangeTracker.Clear();

            if (ownershipLost.IsCancellationRequested)
                return false;

            await RecordFailureAsync(
                message,
                leaseId,
                TruncateError(exception.ToString()),
                cancellationToken);
            return false;
        }
        finally
        {
            if (!stopHeartbeat.IsCancellationRequested)
                await StopHeartbeatAsync(stopHeartbeat, heartbeat);
        }
    }

    private async Task DispatchMessageHandlersAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var payloadType = ModuleEventTypeNames.Resolve(message.EventType);
        var payload = JsonSerializer.Deserialize(message.Payload, payloadType, SerializerOptions)
            ?? throw new InvalidOperationException($"Module event payload '{message.EventType}' deserialized to null.");

        var context = new ModuleEventContext(message.Id, message.EventType, message.OccurredAtUtc);
        foreach (var handler in ResolveHandlers(payloadType))
            await DispatchToHandlerAsync(handler, payloadType, payload, context, cancellationToken);
    }

    private async Task RunLeaseHeartbeatAsync(
        Guid messageId,
        Guid leaseId,
        CancellationToken stoppingToken,
        CancellationTokenSource ownershipLost)
    {
        try
        {
            while (true)
            {
                await Task.Delay(_options.LeaseRenewalInterval, _timeProvider, stoppingToken);

                await using var scope = _scopeFactory.CreateAsyncScope();
                var heartbeatContext = scope.ServiceProvider.GetRequiredService<IModuleEventDbContext>();
                if (!await RenewLeaseAsync(heartbeatContext, messageId, leaseId, stoppingToken))
                {
                    ownershipLost.Cancel();
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to renew lease for module event {MessageId}.", messageId);
            ownershipLost.Cancel();
        }
    }

    private async Task<bool> RenewLeaseAsync(
        IModuleEventDbContext dbContext,
        Guid messageId,
        Guid leaseId,
        CancellationToken cancellationToken)
    {
        var efDbContext = (DbContext)dbContext;
        var leaseExpiresAtUtc = GetUtcNow() + _options.LeaseDuration;
        if (efDbContext.Database.IsRelational())
        {
            return await dbContext.OutboxMessages
                .Where(message =>
                    message.Id == messageId &&
                    message.LeaseId == leaseId &&
                    message.ProcessedAtUtc == null &&
                    message.DeadLetteredAtUtc == null)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(message => message.LeaseExpiresAtUtc, leaseExpiresAtUtc),
                    cancellationToken) == 1;
        }

        await _claimGate.EnterAsync(cancellationToken);
        try
        {
            efDbContext.ChangeTracker.Clear();
            var message = await dbContext.OutboxMessages.SingleOrDefaultAsync(candidate =>
                    candidate.Id == messageId &&
                    candidate.LeaseId == leaseId &&
                    candidate.ProcessedAtUtc == null &&
                    candidate.DeadLetteredAtUtc == null,
                cancellationToken);
            if (message is null)
                return false;

            message.LeaseExpiresAtUtc = leaseExpiresAtUtc;
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        finally
        {
            _claimGate.Exit();
        }
    }

    private async Task<bool> MarkProcessedAsync(
        Guid messageId,
        Guid leaseId,
        CancellationToken cancellationToken)
    {
        var processedAtUtc = GetUtcNow();
        if (_efDbContext.Database.IsRelational())
        {
            return await _dbContext.OutboxMessages
                .Where(message =>
                    message.Id == messageId &&
                    message.LeaseId == leaseId &&
                    message.ProcessedAtUtc == null &&
                    message.DeadLetteredAtUtc == null)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(message => message.ProcessedAtUtc, processedAtUtc)
                        .SetProperty(message => message.LastAttemptAtUtc, processedAtUtc)
                        .SetProperty(message => message.NextAttemptAtUtc, (DateTime?)null)
                        .SetProperty(message => message.LeaseId, (Guid?)null)
                        .SetProperty(message => message.LeaseExpiresAtUtc, (DateTime?)null)
                        .SetProperty(message => message.LastError, (string?)null),
                    cancellationToken) == 1;
        }

        await _claimGate.EnterAsync(cancellationToken);
        try
        {
            _efDbContext.ChangeTracker.Clear();
            var message = await FindOwnedMessageAsync(messageId, leaseId, cancellationToken);
            if (message is null)
                return false;

            message.ProcessedAtUtc = processedAtUtc;
            message.LastAttemptAtUtc = processedAtUtc;
            message.NextAttemptAtUtc = null;
            message.LeaseId = null;
            message.LeaseExpiresAtUtc = null;
            message.LastError = null;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        finally
        {
            _claimGate.Exit();
        }
    }

    private async Task RecordFailureAsync(
        OutboxMessage claimedMessage,
        Guid leaseId,
        string lastError,
        CancellationToken cancellationToken)
    {
        var attemptedAtUtc = GetUtcNow();
        var retryCount = claimedMessage.RetryCount + 1;
        var exhausted = retryCount >= _options.MaxAttempts;
        DateTime? nextAttemptAtUtc = exhausted
            ? null
            : attemptedAtUtc + CalculateRetryDelay(retryCount);
        DateTime? deadLetteredAtUtc = exhausted ? attemptedAtUtc : null;

        if (_efDbContext.Database.IsRelational())
        {
            await _dbContext.OutboxMessages
                .Where(message =>
                    message.Id == claimedMessage.Id &&
                    message.LeaseId == leaseId &&
                    message.ProcessedAtUtc == null &&
                    message.DeadLetteredAtUtc == null)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(message => message.RetryCount, retryCount)
                        .SetProperty(message => message.LastAttemptAtUtc, attemptedAtUtc)
                        .SetProperty(message => message.NextAttemptAtUtc, nextAttemptAtUtc)
                        .SetProperty(message => message.DeadLetteredAtUtc, deadLetteredAtUtc)
                        .SetProperty(message => message.LeaseId, (Guid?)null)
                        .SetProperty(message => message.LeaseExpiresAtUtc, (DateTime?)null)
                        .SetProperty(message => message.LastError, lastError),
                    cancellationToken);
            return;
        }

        await _claimGate.EnterAsync(cancellationToken);
        try
        {
            _efDbContext.ChangeTracker.Clear();
            var message = await FindOwnedMessageAsync(claimedMessage.Id, leaseId, cancellationToken);
            if (message is null)
                return;

            message.RetryCount = retryCount;
            message.LastAttemptAtUtc = attemptedAtUtc;
            message.NextAttemptAtUtc = nextAttemptAtUtc;
            message.DeadLetteredAtUtc = deadLetteredAtUtc;
            message.LeaseId = null;
            message.LeaseExpiresAtUtc = null;
            message.LastError = lastError;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _claimGate.Exit();
        }
    }

    private async Task ReleaseLeaseAsync(
        Guid messageId,
        Guid leaseId,
        CancellationToken cancellationToken)
    {
        if (_efDbContext.Database.IsRelational())
        {
            await _dbContext.OutboxMessages
                .Where(message =>
                    message.Id == messageId &&
                    message.LeaseId == leaseId &&
                    message.ProcessedAtUtc == null &&
                    message.DeadLetteredAtUtc == null)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(message => message.LeaseId, (Guid?)null)
                        .SetProperty(message => message.LeaseExpiresAtUtc, (DateTime?)null),
                    cancellationToken);
            return;
        }

        await _claimGate.EnterAsync(cancellationToken);
        try
        {
            _efDbContext.ChangeTracker.Clear();
            var message = await FindOwnedMessageAsync(messageId, leaseId, cancellationToken);
            if (message is null)
                return;

            message.LeaseId = null;
            message.LeaseExpiresAtUtc = null;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _claimGate.Exit();
        }
    }

    private Task<OutboxMessage?> FindOwnedMessageAsync(
        Guid messageId,
        Guid leaseId,
        CancellationToken cancellationToken) =>
        _dbContext.OutboxMessages.SingleOrDefaultAsync(message =>
                message.Id == messageId &&
                message.LeaseId == leaseId &&
                message.ProcessedAtUtc == null &&
                message.DeadLetteredAtUtc == null,
            cancellationToken);

    private IReadOnlyCollection<object> ResolveHandlers(Type payloadType)
    {
        var handlerInterface = typeof(IModuleEventHandler<>).MakeGenericType(payloadType);
        var enumerableType = typeof(IEnumerable<>).MakeGenericType(handlerInterface);
        var handlers = (IEnumerable)_serviceProvider.GetRequiredService(enumerableType);

        return handlers.Cast<object>().ToArray();
    }

    private async Task DispatchToHandlerAsync(
        object handler,
        Type payloadType,
        object payload,
        ModuleEventContext context,
        CancellationToken cancellationToken)
    {
        var handlerInterface = typeof(IModuleEventHandler<>).MakeGenericType(payloadType);
        var consumerName = (string?)handlerInterface.GetProperty(nameof(IModuleEventHandler<object>.ConsumerName))?.GetValue(handler);
        if (string.IsNullOrWhiteSpace(consumerName))
            throw new InvalidOperationException($"Module event handler '{handler.GetType().FullName}' must provide a consumer name.");

        if (await _dbContext.InboxMessages.AnyAsync(
            inbox => inbox.ConsumerName == consumerName && inbox.MessageId == context.MessageId,
            cancellationToken))
        {
            return;
        }

        var handleMethod = handlerInterface.GetMethod(nameof(IModuleEventHandler<object>.HandleAsync), BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Module event handler '{handler.GetType().FullName}' is missing HandleAsync.");
        var handleTask = (Task?)handleMethod.Invoke(handler, [payload, context, cancellationToken])
            ?? throw new InvalidOperationException($"Module event handler '{handler.GetType().FullName}' returned null.");

        await handleTask;
        _dbContext.InboxMessages.Add(new InboxMessage
        {
            ConsumerName = consumerName,
            MessageId = context.MessageId,
            ProcessedAtUtc = GetUtcNow()
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private TimeSpan CalculateRetryDelay(int retryCount)
    {
        var multiplier = Math.Pow(2, retryCount - 1);
        var delayTicks = Math.Min(
            _options.RetryMaxDelay.Ticks,
            _options.RetryBaseDelay.Ticks * multiplier);
        return TimeSpan.FromTicks((long)delayTicks);
    }

    private DateTime GetUtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

    private static async Task StopHeartbeatAsync(CancellationTokenSource stopHeartbeat, Task heartbeat)
    {
        await stopHeartbeat.CancelAsync();
        await heartbeat;
    }

    private static string TruncateError(string error) =>
        error.Length <= LastErrorMaxLength
            ? error
            : error[..LastErrorMaxLength];

    private sealed record TerminalMessageCandidate(Guid Id, DateTime TerminalAtUtc);
}
