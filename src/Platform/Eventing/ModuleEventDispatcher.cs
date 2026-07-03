using System.Collections;
using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Platform.Eventing.Persistence;

namespace Platform.Eventing;

internal sealed class ModuleEventDispatcher : IModuleEventDispatcher
{
    private const int LastErrorMaxLength = 4000;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IModuleEventDbContext _dbContext;
    private readonly IServiceProvider _serviceProvider;

    public ModuleEventDispatcher(
        IModuleEventDbContext dbContext,
        IServiceProvider serviceProvider)
    {
        _dbContext = dbContext;
        _serviceProvider = serviceProvider;
    }

    public async Task<int> DispatchPendingAsync(int batchSize = 50, CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(batchSize), "Batch size must be greater than zero.");

        var messages = await _dbContext.OutboxMessages
            .Where(message => message.ProcessedAtUtc == null)
            .OrderBy(message => message.OccurredAtUtc)
            .ThenBy(message => message.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        var processedCount = 0;
        foreach (var message in messages)
        {
            if (await DispatchMessageAsync(message, cancellationToken))
                processedCount++;
        }

        return processedCount;
    }

    private async Task<bool> DispatchMessageAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var payloadType = ModuleEventTypeNames.Resolve(message.EventType);
            var payload = JsonSerializer.Deserialize(message.Payload, payloadType, SerializerOptions)
                ?? throw new InvalidOperationException($"Module event payload '{message.EventType}' deserialized to null.");

            var context = new ModuleEventContext(message.Id, message.EventType, message.OccurredAtUtc);
            // Each handler commits its own inbox marker, so retries skip already completed consumers.
            foreach (var handler in ResolveHandlers(payloadType))
                await DispatchToHandlerAsync(handler, payloadType, payload, context, cancellationToken);

            message.ProcessedAtUtc = DateTime.UtcNow;
            message.LastAttemptAtUtc = message.ProcessedAtUtc;
            message.LastError = null;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception exception)
        {
            await SaveFailedDispatchStateAsync(message.Id, TruncateError(exception.ToString()), cancellationToken);
            return false;
        }
    }

    private async Task SaveFailedDispatchStateAsync(
        Guid messageId,
        string lastError,
        CancellationToken cancellationToken)
    {
        if (_dbContext is DbContext dbContext)
            dbContext.ChangeTracker.Clear();

        var message = await _dbContext.OutboxMessages
            .SingleAsync(outbox => outbox.Id == messageId, cancellationToken);

        message.RetryCount++;
        message.LastAttemptAtUtc = DateTime.UtcNow;
        message.LastError = lastError;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

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
            ProcessedAtUtc = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string TruncateError(string error)
    {
        return error.Length <= LastErrorMaxLength
            ? error
            : error[..LastErrorMaxLength];
    }
}
