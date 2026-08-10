using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Platform.Eventing;
using Platform.Eventing.Persistence;
using Platform.Extensions;

namespace Platform.Tests;

public sealed class ModuleEventingReliabilityTests
{
    [Fact]
    public async Task ConcurrentDispatchers_DoNotOverlapHandlerBeyondInitialLease()
    {
        var state = new ReliabilityState();
        await using var provider = CreateProvider(
            state,
            services => services.AddModuleEventHandler<ReliabilityEvent, BlockingHandler>(),
            options => options.LeaseDuration = TimeSpan.FromMilliseconds(120));
        await PublishAsync(provider, new ReliabilityEvent("slow"));
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var firstDispatch = firstScope.ServiceProvider
            .GetRequiredService<IModuleEventDispatcher>()
            .DispatchPendingAsync(1);
        await state.Entered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await Task.Delay(TimeSpan.FromMilliseconds(320));

        var secondProcessed = await secondScope.ServiceProvider
            .GetRequiredService<IModuleEventDispatcher>()
            .DispatchPendingAsync(1);

        Assert.Equal(0, secondProcessed);
        Assert.Equal(1, Volatile.Read(ref state.HandlerCalls));
        Assert.Equal(1, Volatile.Read(ref state.MaxConcurrentHandlers));

        state.Release.TrySetResult();
        Assert.Equal(1, await firstDispatch.WaitAsync(TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public async Task Dispatcher_RecoversExpiredLease()
    {
        var now = new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new MutableTimeProvider(now);
        var state = new ReliabilityState { ReleaseImmediately = true };
        await using var provider = CreateProvider(
            state,
            services => services.AddModuleEventHandler<ReliabilityEvent, BlockingHandler>(),
            timeProvider: timeProvider);
        var messageId = await PublishAsync(provider, new ReliabilityEvent("recover"), now.UtcDateTime);
        await using (var setupScope = provider.CreateAsyncScope())
        {
            var dbContext = setupScope.ServiceProvider.GetRequiredService<MercuriusDBContext>();
            var message = await dbContext.OutboxMessages.SingleAsync(candidate => candidate.Id == messageId);
            message.LeaseId = Guid.NewGuid();
            message.LeaseExpiresAtUtc = now.UtcDateTime.AddSeconds(-1);
            await dbContext.SaveChangesAsync();
        }

        await using var dispatchScope = provider.CreateAsyncScope();
        var processed = await dispatchScope.ServiceProvider
            .GetRequiredService<IModuleEventDispatcher>()
            .DispatchPendingAsync(1);

        Assert.Equal(1, processed);
        Assert.Equal(1, Volatile.Read(ref state.HandlerCalls));
        var stored = await dispatchScope.ServiceProvider
            .GetRequiredService<MercuriusDBContext>()
            .OutboxMessages
            .SingleAsync(candidate => candidate.Id == messageId);
        Assert.NotNull(stored.ProcessedAtUtc);
        Assert.Null(stored.LeaseId);
        Assert.Null(stored.LeaseExpiresAtUtc);
    }

    [Fact]
    public async Task Dispatcher_ThatLosesOwnershipCannotFinalizeMessage()
    {
        var state = new ReliabilityState();
        await using var provider = CreateProvider(
            state,
            services => services.AddModuleEventHandler<ReliabilityEvent, BlockingHandler>(),
            options => options.LeaseDuration = TimeSpan.FromMilliseconds(120));
        var messageId = await PublishAsync(provider, new ReliabilityEvent("ownership"));
        using var dispatchScope = provider.CreateScope();
        var dispatch = dispatchScope.ServiceProvider
            .GetRequiredService<IModuleEventDispatcher>()
            .DispatchPendingAsync(1);
        await state.Entered.Task.WaitAsync(TimeSpan.FromSeconds(2));

        var replacementLease = Guid.NewGuid();
        await using (var takeoverScope = provider.CreateAsyncScope())
        {
            var dbContext = takeoverScope.ServiceProvider.GetRequiredService<MercuriusDBContext>();
            var message = await dbContext.OutboxMessages.SingleAsync(candidate => candidate.Id == messageId);
            message.LeaseId = replacementLease;
            message.LeaseExpiresAtUtc = DateTime.UtcNow.AddMinutes(1);
            await dbContext.SaveChangesAsync();
        }

        Assert.Equal(0, await dispatch.WaitAsync(TimeSpan.FromSeconds(2)));
        await using var verificationScope = provider.CreateAsyncScope();
        var stored = await verificationScope.ServiceProvider
            .GetRequiredService<MercuriusDBContext>()
            .OutboxMessages
            .SingleAsync(candidate => candidate.Id == messageId);
        Assert.Null(stored.ProcessedAtUtc);
        Assert.Equal(replacementLease, stored.LeaseId);
        Assert.Equal(0, stored.RetryCount);
    }

    [Fact]
    public async Task Dispatcher_SchedulesCappedBackoffAndDeadLettersAtMaximumAttempts()
    {
        var now = new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new MutableTimeProvider(now);
        var state = new ReliabilityState { ThrowAlways = true };
        await using var provider = CreateProvider(
            state,
            services => services.AddModuleEventHandler<ReliabilityEvent, ThrowingHandler>(),
            options =>
            {
                options.MaxAttempts = 4;
                options.RetryBaseDelay = TimeSpan.FromSeconds(2);
                options.RetryMaxDelay = TimeSpan.FromSeconds(5);
            },
            timeProvider);
        var messageId = await PublishAsync(provider, new ReliabilityEvent("poison"), now.UtcDateTime);
        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IModuleEventDispatcher>();
        var dbContext = scope.ServiceProvider.GetRequiredService<MercuriusDBContext>();

        Assert.Equal(0, await dispatcher.DispatchPendingAsync(1));
        var message = await LoadMessageAsync(dbContext, messageId);
        Assert.Equal(1, message.RetryCount);
        Assert.Equal(now.UtcDateTime.AddSeconds(2), message.NextAttemptAtUtc);
        Assert.Equal(0, await dispatcher.DispatchPendingAsync(1));
        Assert.Equal(1, Volatile.Read(ref state.HandlerCalls));

        timeProvider.Advance(TimeSpan.FromSeconds(2));
        Assert.Equal(0, await dispatcher.DispatchPendingAsync(1));
        message = await LoadMessageAsync(dbContext, messageId);
        Assert.Equal(2, message.RetryCount);
        Assert.Equal(now.UtcDateTime.AddSeconds(6), message.NextAttemptAtUtc);

        timeProvider.Advance(TimeSpan.FromSeconds(4));
        Assert.Equal(0, await dispatcher.DispatchPendingAsync(1));
        message = await LoadMessageAsync(dbContext, messageId);
        Assert.Equal(3, message.RetryCount);
        Assert.Equal(now.UtcDateTime.AddSeconds(11), message.NextAttemptAtUtc);

        timeProvider.Advance(TimeSpan.FromSeconds(5));
        Assert.Equal(0, await dispatcher.DispatchPendingAsync(1));
        message = await LoadMessageAsync(dbContext, messageId);
        Assert.Equal(4, message.RetryCount);
        Assert.Equal(now.UtcDateTime.AddSeconds(11), message.DeadLetteredAtUtc);
        Assert.Null(message.NextAttemptAtUtc);
        Assert.Null(message.LeaseId);
        Assert.Contains("planned reliability failure", message.LastError, StringComparison.Ordinal);

        timeProvider.Advance(TimeSpan.FromDays(1));
        Assert.Equal(0, await dispatcher.DispatchPendingAsync(1));
        Assert.Equal(4, Volatile.Read(ref state.HandlerCalls));
    }

    [Fact]
    public async Task Dispatcher_PoisonMessagesAcrossBatchBoundaryDoNotStarveHealthyMessage()
    {
        var now = new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc);
        var timeProvider = new MutableTimeProvider(new DateTimeOffset(now));
        var state = new ReliabilityState();
        await using var provider = CreateProvider(
            state,
            services => services.AddModuleEventHandler<ReliabilityEvent, PoisonAwareHandler>(),
            options =>
            {
                options.RetryBaseDelay = TimeSpan.FromHours(1);
                options.RetryMaxDelay = TimeSpan.FromHours(1);
            },
            timeProvider);

        for (var index = 0; index < 5; index++)
        {
            await PublishAsync(
                provider,
                new ReliabilityEvent($"poison-{index}", IsPoison: true),
                now.AddMinutes(-10).AddSeconds(index));
        }

        var healthyId = await PublishAsync(
            provider,
            new ReliabilityEvent("healthy"),
            now.AddMinutes(-10).AddSeconds(5));
        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IModuleEventDispatcher>();

        Assert.Equal(0, await dispatcher.DispatchPendingAsync(3));
        Assert.Equal(1, await dispatcher.DispatchPendingAsync(3));

        var healthy = await LoadMessageAsync(
            scope.ServiceProvider.GetRequiredService<MercuriusDBContext>(),
            healthyId);
        Assert.NotNull(healthy.ProcessedAtUtc);
        Assert.Equal(6, Volatile.Read(ref state.HandlerCalls));
    }

    [Fact]
    public async Task CleanupTerminalAsync_DeletesBoundedExpiredRowsAndOnlyTheirInboxMarkers()
    {
        var now = new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new MutableTimeProvider(now);
        var state = new ReliabilityState();
        await using var provider = CreateProvider(
            state,
            _ => { },
            options =>
            {
                options.CleanupBatchSize = 2;
                options.SuccessfulRetention = TimeSpan.FromDays(1);
                options.DeadLetterRetention = TimeSpan.FromDays(2);
            },
            timeProvider);
        var deadLetteredId = Guid.NewGuid();
        var oldestProcessedId = Guid.NewGuid();
        var otherProcessedId = Guid.NewGuid();
        var recentProcessedId = Guid.NewGuid();
        var pendingId = Guid.NewGuid();
        var records = new[]
        {
            CreateOutbox(deadLetteredId, now.UtcDateTime.AddDays(-10), deadLetteredAtUtc: now.UtcDateTime.AddDays(-10)),
            CreateOutbox(oldestProcessedId, now.UtcDateTime.AddDays(-9), processedAtUtc: now.UtcDateTime.AddDays(-9)),
            CreateOutbox(otherProcessedId, now.UtcDateTime.AddDays(-8), processedAtUtc: now.UtcDateTime.AddDays(-8)),
            CreateOutbox(recentProcessedId, now.UtcDateTime.AddHours(-1), processedAtUtc: now.UtcDateTime.AddHours(-1)),
            CreateOutbox(pendingId, now.UtcDateTime.AddDays(-30))
        };
        await using (var setupScope = provider.CreateAsyncScope())
        {
            var setupDbContext = setupScope.ServiceProvider.GetRequiredService<MercuriusDBContext>();
            setupDbContext.OutboxMessages.AddRange(records);
            setupDbContext.InboxMessages.AddRange(records.Select(message => new InboxMessage
            {
                ConsumerName = "cleanup-consumer",
                MessageId = message.Id,
                ProcessedAtUtc = message.OccurredAtUtc
            }));
            await setupDbContext.SaveChangesAsync();
        }

        await using var cleanupScope = provider.CreateAsyncScope();
        var deleted = await cleanupScope.ServiceProvider
            .GetRequiredService<IModuleEventDispatcher>()
            .CleanupTerminalAsync();

        Assert.Equal(2, deleted);
        var dbContext = cleanupScope.ServiceProvider.GetRequiredService<MercuriusDBContext>();
        var remainingOutboxIds = await dbContext.OutboxMessages
            .AsNoTracking()
            .Select(message => message.Id)
            .ToListAsync();
        var remainingInboxIds = await dbContext.InboxMessages
            .AsNoTracking()
            .Select(message => message.MessageId)
            .ToListAsync();
        Assert.DoesNotContain(deadLetteredId, remainingOutboxIds);
        Assert.DoesNotContain(oldestProcessedId, remainingOutboxIds);
        Assert.Contains(otherProcessedId, remainingOutboxIds);
        Assert.Contains(recentProcessedId, remainingOutboxIds);
        Assert.Contains(pendingId, remainingOutboxIds);
        Assert.Equal(remainingOutboxIds.Order(), remainingInboxIds.Order());
    }

    [Fact]
    public async Task Dispatcher_CancellationReleasesLeaseWithoutRecordingFailure()
    {
        var state = new ReliabilityState();
        await using var provider = CreateProvider(
            state,
            services => services.AddModuleEventHandler<ReliabilityEvent, BlockingHandler>(),
            options => options.LeaseDuration = TimeSpan.FromSeconds(1));
        var messageId = await PublishAsync(provider, new ReliabilityEvent("cancel"));
        using var scope = provider.CreateScope();
        using var cancellation = new CancellationTokenSource();
        var dispatch = scope.ServiceProvider
            .GetRequiredService<IModuleEventDispatcher>()
            .DispatchPendingAsync(1, cancellation.Token);
        await state.Entered.Task.WaitAsync(TimeSpan.FromSeconds(2));

        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dispatch);

        var message = await LoadMessageAsync(
            scope.ServiceProvider.GetRequiredService<MercuriusDBContext>(),
            messageId);
        Assert.Equal(0, message.RetryCount);
        Assert.Null(message.LeaseId);
        Assert.Null(message.LeaseExpiresAtUtc);
        Assert.Null(message.ProcessedAtUtc);
        Assert.Null(message.DeadLetteredAtUtc);
    }

    [Fact]
    public void AddModuleEventing_BindsValidatedOptionsAndRegistersWorker()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ModuleEventing:DispatchBatchSize"] = "17",
                ["ModuleEventing:PollInterval"] = "00:00:03",
                ["ModuleEventing:LeaseDuration"] = "00:00:12",
                ["ModuleEventing:MaxAttempts"] = "7",
                ["ModuleEventing:RetryBaseDelay"] = "00:00:04",
                ["ModuleEventing:RetryMaxDelay"] = "00:00:40",
                ["ModuleEventing:SuccessfulRetention"] = "3.00:00:00",
                ["ModuleEventing:DeadLetterRetention"] = "20.00:00:00",
                ["ModuleEventing:CleanupBatchSize"] = "23",
                ["ModuleEventing:CleanupInterval"] = "02:00:00"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<MercuriusDBContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddModuleEventing<MercuriusDBContext>(configuration);
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<ModuleEventingOptions>>().Value;
        Assert.Equal(17, options.DispatchBatchSize);
        Assert.Equal(TimeSpan.FromSeconds(3), options.PollInterval);
        Assert.Equal(TimeSpan.FromSeconds(12), options.LeaseDuration);
        Assert.Equal(7, options.MaxAttempts);
        Assert.Equal(TimeSpan.FromSeconds(4), options.RetryBaseDelay);
        Assert.Equal(TimeSpan.FromSeconds(40), options.RetryMaxDelay);
        Assert.Equal(TimeSpan.FromDays(3), options.SuccessfulRetention);
        Assert.Equal(TimeSpan.FromDays(20), options.DeadLetterRetention);
        Assert.Equal(23, options.CleanupBatchSize);
        Assert.Equal(TimeSpan.FromHours(2), options.CleanupInterval);
        Assert.IsType<ModuleEventDispatchWorker>(Assert.Single(provider.GetServices<IHostedService>()));
        Assert.NotNull(provider.GetRequiredService<TimeProvider>());
    }

    [Fact]
    public void AddModuleEventing_RejectsRetryMaximumBelowBaseDelay()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ModuleEventing:RetryBaseDelay"] = "00:01:00",
                ["ModuleEventing:RetryMaxDelay"] = "00:00:30"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddModuleEventing<MercuriusDBContext>(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IOptions<ModuleEventingOptions>>().Value);
    }

    [Fact]
    public void PostgreSqlClaim_AtomicallyUpdatesLeaseWithSkipLocked()
    {
        Assert.Contains("FOR UPDATE SKIP LOCKED", ModuleEventDispatcher.PostgreSqlClaimSql, StringComparison.Ordinal);
        Assert.Contains("UPDATE platform.outbox_messages", ModuleEventDispatcher.PostgreSqlClaimSql, StringComparison.Ordinal);
        Assert.Contains("lease_id = {1}", ModuleEventDispatcher.PostgreSqlClaimSql, StringComparison.Ordinal);
        Assert.Contains("dead_lettered_at_utc IS NULL", ModuleEventDispatcher.PostgreSqlClaimSql, StringComparison.Ordinal);
        Assert.Contains("next_attempt_at_utc <= {0}", ModuleEventDispatcher.PostgreSqlClaimSql, StringComparison.Ordinal);
        Assert.Contains("lease_expires_at_utc <= {0}", ModuleEventDispatcher.PostgreSqlClaimSql, StringComparison.Ordinal);
        Assert.Contains("RETURNING message.id", ModuleEventDispatcher.PostgreSqlClaimSql, StringComparison.Ordinal);
    }

    [Fact]
    public void HardenModuleEventDispatchMigration_AddsLifecycleColumnsAndIndexes()
    {
        var migration = new HardenModuleEventDispatch();
        var operations = migration.UpOperations.ToList();
        var addedColumns = operations
            .OfType<AddColumnOperation>()
            .Where(operation => operation.Schema == "platform" && operation.Table == "outbox_messages")
            .Select(operation => operation.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Equal(
            new HashSet<string>(StringComparer.Ordinal)
            {
                "dead_lettered_at_utc",
                "lease_expires_at_utc",
                "lease_id",
                "next_attempt_at_utc"
            },
            addedColumns);
        var indexes = operations.OfType<CreateIndexOperation>().ToList();
        Assert.Contains(indexes, index =>
            index.Name == "IX_outbox_messages_pending_claim" &&
            index.Columns.SequenceEqual(["next_attempt_at_utc", "lease_expires_at_utc", "occurred_at_utc", "id"]) &&
            index.Filter == "processed_at_utc IS NULL AND dead_lettered_at_utc IS NULL");
        Assert.Contains(indexes, index => index.Name == "IX_outbox_messages_processed_retention");
        Assert.Contains(indexes, index => index.Name == "IX_outbox_messages_dead_letter_retention");
        Assert.Contains(indexes, index =>
            index.Name == "IX_inbox_messages_message_id" &&
            index.Columns.SequenceEqual(["message_id"]));
    }

    [Fact]
    public void EventingModel_ContainsLifecyclePropertiesAndPurposeBuiltIndexes()
    {
        var options = new DbContextOptionsBuilder<MercuriusDBContext>()
            .UseNpgsql("Host=localhost;Database=translation-only")
            .Options;
        using var dbContext = new MercuriusDBContext(options);
        var outbox = dbContext.Model.FindEntityType(typeof(OutboxMessage));

        Assert.NotNull(outbox?.FindProperty(nameof(OutboxMessage.NextAttemptAtUtc)));
        Assert.NotNull(outbox?.FindProperty(nameof(OutboxMessage.LeaseId)));
        Assert.NotNull(outbox?.FindProperty(nameof(OutboxMessage.LeaseExpiresAtUtc)));
        Assert.NotNull(outbox?.FindProperty(nameof(OutboxMessage.DeadLetteredAtUtc)));
        Assert.Contains(outbox!.GetIndexes(), index =>
            index.GetDatabaseName() == "IX_outbox_messages_pending_claim" &&
            index.GetFilter() == "processed_at_utc IS NULL AND dead_lettered_at_utc IS NULL");
        Assert.Contains(outbox.GetIndexes(), index => index.GetDatabaseName() == "IX_outbox_messages_processed_retention");
        Assert.Contains(outbox.GetIndexes(), index => index.GetDatabaseName() == "IX_outbox_messages_dead_letter_retention");

        var inbox = dbContext.Model.FindEntityType(typeof(InboxMessage));
        Assert.Contains(inbox!.GetIndexes(), index => index.GetDatabaseName() == "IX_inbox_messages_message_id");
    }

    private static async Task<Guid> PublishAsync(
        ServiceProvider provider,
        ReliabilityEvent payload,
        DateTime? occurredAtUtc = null)
    {
        await using var scope = provider.CreateAsyncScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IModuleEventPublisher>();
        var dbContext = scope.ServiceProvider.GetRequiredService<MercuriusDBContext>();
        var messageId = publisher.Publish(payload, occurredAtUtc);
        await dbContext.SaveChangesAsync();
        return messageId;
    }

    private static ServiceProvider CreateProvider(
        ReliabilityState state,
        Action<IServiceCollection> configureHandlers,
        Action<ModuleEventingOptions>? options = null,
        TimeProvider? timeProvider = null)
    {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString();
        services.AddLogging();
        if (timeProvider is not null)
            services.AddSingleton(timeProvider);
        services.AddDbContext<MercuriusDBContext>(builder =>
            builder.UseInMemoryDatabase(databaseName));
        services.AddSingleton(state);
        services.AddModuleEventing<MercuriusDBContext>();
        if (options is not null)
            services.Configure(options);
        configureHandlers(services);
        return services.BuildServiceProvider();
    }

    private static async Task<OutboxMessage> LoadMessageAsync(
        MercuriusDBContext dbContext,
        Guid messageId)
    {
        dbContext.ChangeTracker.Clear();
        return await dbContext.OutboxMessages.SingleAsync(message => message.Id == messageId);
    }

    private static OutboxMessage CreateOutbox(
        Guid id,
        DateTime occurredAtUtc,
        DateTime? processedAtUtc = null,
        DateTime? deadLetteredAtUtc = null) =>
        new()
        {
            Id = id,
            EventType = typeof(ReliabilityEvent).FullName!,
            Payload = "{}",
            OccurredAtUtc = occurredAtUtc,
            NextAttemptAtUtc = processedAtUtc.HasValue || deadLetteredAtUtc.HasValue ? null : occurredAtUtc,
            ProcessedAtUtc = processedAtUtc,
            DeadLetteredAtUtc = deadLetteredAtUtc
        };

    private sealed record ReliabilityEvent(string Name, bool IsPoison = false);

    private sealed class ReliabilityState
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool ReleaseImmediately { get; init; }
        public bool ThrowAlways { get; init; }
        public int HandlerCalls;
        public int ActiveHandlers;
        public int MaxConcurrentHandlers;
    }

    private sealed class BlockingHandler : IModuleEventHandler<ReliabilityEvent>
    {
        private readonly ReliabilityState _state;

        public BlockingHandler(ReliabilityState state)
        {
            _state = state;
        }

        public string ConsumerName => "reliability-blocking";

        public async Task HandleAsync(
            ReliabilityEvent payload,
            ModuleEventContext context,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _state.HandlerCalls);
            var active = Interlocked.Increment(ref _state.ActiveHandlers);
            UpdateMaximum(ref _state.MaxConcurrentHandlers, active);
            _state.Entered.TrySetResult();
            try
            {
                if (!_state.ReleaseImmediately)
                    await _state.Release.Task.WaitAsync(cancellationToken);
            }
            finally
            {
                Interlocked.Decrement(ref _state.ActiveHandlers);
            }
        }
    }

    private sealed class ThrowingHandler : IModuleEventHandler<ReliabilityEvent>
    {
        private readonly ReliabilityState _state;

        public ThrowingHandler(ReliabilityState state)
        {
            _state = state;
        }

        public string ConsumerName => "reliability-throwing";

        public Task HandleAsync(
            ReliabilityEvent payload,
            ModuleEventContext context,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _state.HandlerCalls);
            if (_state.ThrowAlways)
                throw new InvalidOperationException("planned reliability failure");

            return Task.CompletedTask;
        }
    }

    private sealed class PoisonAwareHandler : IModuleEventHandler<ReliabilityEvent>
    {
        private readonly ReliabilityState _state;

        public PoisonAwareHandler(ReliabilityState state)
        {
            _state = state;
        }

        public string ConsumerName => "reliability-poison-aware";

        public Task HandleAsync(
            ReliabilityEvent payload,
            ModuleEventContext context,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _state.HandlerCalls);
            if (payload.IsPoison)
                throw new InvalidOperationException("planned poison failure");

            return Task.CompletedTask;
        }
    }

    private sealed class MutableTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public MutableTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan elapsed) => _utcNow += elapsed;
    }

    private static void UpdateMaximum(ref int maximum, int candidate)
    {
        var observed = Volatile.Read(ref maximum);
        while (candidate > observed)
        {
            var original = Interlocked.CompareExchange(ref maximum, candidate, observed);
            if (original == observed)
                return;

            observed = original;
        }
    }
}
