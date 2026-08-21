using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;
using Platform.Eventing;
using Platform.Eventing.Persistence;
using Platform.Extensions;

namespace Platform.Tests;

public sealed class ModuleEventingReliabilityTests
{
    [Fact]
    public async Task Dispatcher_PersistsLaterSuccessAfterEarlierFailure()
    {
        var now = new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
        var state = new ReliabilityState();
        await using var provider = CreateProvider(state, new MutableTimeProvider(now));
        var poisonId = await PublishAsync(provider, "poison", now.UtcDateTime.AddMinutes(-1), isPoison: true);
        var healthyId = await PublishAsync(provider, "healthy", now.UtcDateTime.AddSeconds(-59));

        await using var scope = provider.CreateAsyncScope();
        var processed = await scope.ServiceProvider
            .GetRequiredService<IModuleEventDispatcher>()
            .DispatchPendingAsync(2);

        Assert.Equal(1, processed);
        Assert.Equal(["poison", "healthy"], state.Attempts);
        var poison = await LoadMessageAsync(provider, poisonId);
        var healthy = await LoadMessageAsync(provider, healthyId);
        Assert.Equal(1, poison.RetryCount);
        Assert.Null(poison.ProcessedAtUtc);
        Assert.NotNull(healthy.ProcessedAtUtc);
    }

    [Fact]
    public async Task Dispatcher_DefersRetriesAndDeadLettersAfterFifthFailure()
    {
        var now = new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
        var timeProvider = new MutableTimeProvider(now);
        var state = new ReliabilityState();
        await using var provider = CreateProvider(state, timeProvider);
        var messageId = await PublishAsync(provider, "poison", now.UtcDateTime, isPoison: true);
        await using var scope = provider.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IModuleEventDispatcher>();

        Assert.Equal(0, await dispatcher.DispatchPendingAsync(1));
        var message = await LoadMessageAsync(provider, messageId);
        Assert.Equal(now.UtcDateTime.AddSeconds(5), message.NextAttemptAtUtc);

        Assert.Equal(0, await dispatcher.DispatchPendingAsync(1));
        Assert.Single(state.Attempts);

        foreach (var delay in new[] { 5, 10, 20 })
        {
            timeProvider.Advance(TimeSpan.FromSeconds(delay));
            Assert.Equal(0, await dispatcher.DispatchPendingAsync(1));
        }

        message = await LoadMessageAsync(provider, messageId);
        Assert.Equal(4, message.RetryCount);
        Assert.Equal(now.UtcDateTime.AddSeconds(75), message.NextAttemptAtUtc);

        timeProvider.Advance(TimeSpan.FromSeconds(40));
        Assert.Equal(0, await dispatcher.DispatchPendingAsync(1));
        message = await LoadMessageAsync(provider, messageId);
        Assert.Equal(5, message.RetryCount);
        Assert.Equal(now.UtcDateTime.AddSeconds(75), message.LastAttemptAtUtc);
        Assert.Equal(now.UtcDateTime.AddSeconds(75), message.DeadLetteredAtUtc);
        Assert.Null(message.NextAttemptAtUtc);
        Assert.Contains("planned poison failure", message.LastError, StringComparison.Ordinal);

        timeProvider.Advance(TimeSpan.FromDays(1));
        Assert.Equal(0, await dispatcher.DispatchPendingAsync(1));
        Assert.Equal(5, state.Attempts.Count);
    }

    [Fact]
    public async Task Dispatcher_PoisonMessagesAcrossBatchBoundaryDoNotStarveHealthyMessage()
    {
        var now = new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
        var state = new ReliabilityState();
        await using var provider = CreateProvider(state, new MutableTimeProvider(now));

        for (var index = 0; index < 5; index++)
        {
            await PublishAsync(
                provider,
                $"poison-{index}",
                now.UtcDateTime.AddMinutes(-1).AddSeconds(index),
                isPoison: true);
        }

        var healthyId = await PublishAsync(provider, "healthy", now.UtcDateTime.AddSeconds(-55));
        await using var scope = provider.CreateAsyncScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IModuleEventDispatcher>();

        Assert.Equal(0, await dispatcher.DispatchPendingAsync(3));
        Assert.Equal(1, await dispatcher.DispatchPendingAsync(3));

        Assert.Equal(
            ["poison-0", "poison-1", "poison-2", "poison-3", "poison-4", "healthy"],
            state.Attempts);
        Assert.NotNull((await LoadMessageAsync(provider, healthyId)).ProcessedAtUtc);
    }

    [Fact]
    public async Task Dispatcher_CancellationPropagatesWithoutRecordingFailure()
    {
        var now = new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero);
        var state = new ReliabilityState { Block = true };
        await using var provider = CreateProvider(state, new MutableTimeProvider(now));
        var messageId = await PublishAsync(provider, "blocked", now.UtcDateTime);
        await using var scope = provider.CreateAsyncScope();
        using var cancellation = new CancellationTokenSource();

        var dispatch = scope.ServiceProvider
            .GetRequiredService<IModuleEventDispatcher>()
            .DispatchPendingAsync(1, cancellation.Token);
        await state.Entered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dispatch);
        var message = await LoadMessageAsync(provider, messageId);
        Assert.Equal(0, message.RetryCount);
        Assert.Null(message.LastAttemptAtUtc);
        Assert.Null(message.LastError);
        Assert.Null(message.ProcessedAtUtc);
        Assert.Null(message.DeadLetteredAtUtc);
    }

    [Fact]
    public void HardeningMigrationAndModels_ContainOnlyRetryLifecycleSchema()
    {
        var migration = new HardenModuleEventDispatch();
        var operations = migration.UpOperations.ToList();
        var addedColumns = operations
            .OfType<AddColumnOperation>()
            .Where(operation => operation.Schema == "platform" && operation.Table == "outbox_messages")
            .Select(operation => operation.Name)
            .ToArray();

        Assert.Equal(["dead_lettered_at_utc", "next_attempt_at_utc"], addedColumns);
        var index = Assert.Single(operations.OfType<CreateIndexOperation>());
        Assert.Equal("IX_outbox_messages_pending_dispatch", index.Name);
        Assert.Equal(["next_attempt_at_utc", "occurred_at_utc", "id"], index.Columns);
        Assert.Equal("processed_at_utc IS NULL AND dead_lettered_at_utc IS NULL", index.Filter);
        AssertRetryLifecycleModel(migration.TargetModel);

        var options = new DbContextOptionsBuilder<MercuriusDBContext>()
            .UseNpgsql("Host=localhost;Database=model-only")
            .Options;
        using var dbContext = new MercuriusDBContext(options);
        AssertRetryLifecycleModel(dbContext.Model);
    }

    private static void AssertRetryLifecycleModel(IModel model)
    {
        var outbox = Assert.IsAssignableFrom<IReadOnlyEntityType>(
            model.FindEntityType(typeof(OutboxMessage)));
        Assert.NotNull(outbox.FindProperty(nameof(OutboxMessage.NextAttemptAtUtc)));
        Assert.NotNull(outbox.FindProperty(nameof(OutboxMessage.DeadLetteredAtUtc)));
        Assert.Null(outbox.FindProperty("LeaseId"));
        Assert.Null(outbox.FindProperty("LeaseExpiresAtUtc"));

        var index = Assert.Single(outbox.GetIndexes());
        Assert.Equal("IX_outbox_messages_pending_dispatch", index.GetDatabaseName());
        Assert.Equal(
            [nameof(OutboxMessage.NextAttemptAtUtc), nameof(OutboxMessage.OccurredAtUtc), nameof(OutboxMessage.Id)],
            index.Properties.Select(property => property.Name));
        Assert.Equal("processed_at_utc IS NULL AND dead_lettered_at_utc IS NULL", index.GetFilter());

        var inbox = Assert.IsAssignableFrom<IReadOnlyEntityType>(
            model.FindEntityType(typeof(InboxMessage)));
        Assert.Empty(inbox.GetIndexes());
    }

    private static async Task<Guid> PublishAsync(
        ServiceProvider provider,
        string name,
        DateTime occurredAtUtc,
        bool isPoison = false)
    {
        await using var scope = provider.CreateAsyncScope();
        var messageId = scope.ServiceProvider
            .GetRequiredService<IModuleEventPublisher>()
            .Publish(new ReliabilityEvent(name, isPoison), occurredAtUtc);
        await scope.ServiceProvider.GetRequiredService<MercuriusDBContext>().SaveChangesAsync();
        return messageId;
    }

    private static async Task<OutboxMessage> LoadMessageAsync(ServiceProvider provider, Guid messageId)
    {
        await using var scope = provider.CreateAsyncScope();
        return await scope.ServiceProvider
            .GetRequiredService<MercuriusDBContext>()
            .OutboxMessages
            .AsNoTracking()
            .SingleAsync(message => message.Id == messageId);
    }

    private static ServiceProvider CreateProvider(ReliabilityState state, TimeProvider timeProvider)
    {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString();
        services.AddLogging();
        services.AddDbContext<MercuriusDBContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        services.AddSingleton(state);
        services.AddSingleton(timeProvider);
        services.AddModuleEventing<MercuriusDBContext>();
        services.AddModuleEventHandler<ReliabilityEvent, ReliabilityHandler>();
        return services.BuildServiceProvider();
    }

    private sealed record ReliabilityEvent(string Name, bool IsPoison);

    private sealed class ReliabilityState
    {
        public List<string> Attempts { get; } = [];
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Block { get; init; }
    }

    private sealed class ReliabilityHandler : IModuleEventHandler<ReliabilityEvent>
    {
        private readonly ReliabilityState _state;

        public ReliabilityHandler(ReliabilityState state)
        {
            _state = state;
        }

        public string ConsumerName => "reliability-handler";

        public async Task HandleAsync(
            ReliabilityEvent payload,
            ModuleEventContext context,
            CancellationToken cancellationToken = default)
        {
            _state.Attempts.Add(payload.Name);
            if (payload.IsPoison)
                throw new InvalidOperationException("planned poison failure");

            if (_state.Block)
            {
                _state.Entered.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
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
}
