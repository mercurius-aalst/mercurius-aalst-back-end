using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Platform.Eventing.Persistence;

namespace Platform.Eventing;

internal sealed class ModuleEventDispatcher : IModuleEventDispatcher
{
    private const int LastErrorMaxLength = 4000;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IModuleEventDbContext _dbContext;
    private readonly ModuleEventTypeRegistry _eventTypes;
    private readonly IReadOnlyCollection<IModuleEventHandlerInvoker> _handlers;

    public ModuleEventDispatcher(
        IModuleEventDbContext dbContext,
        ModuleEventTypeRegistry eventTypes,
        IEnumerable<IModuleEventHandlerInvoker> handlers)
    {
        _dbContext = dbContext;
        _eventTypes = eventTypes;
        _handlers = handlers.ToArray();
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
            var payloadType = _eventTypes.GetPayloadType(message.EventType);
            var payload = JsonSerializer.Deserialize(message.Payload, payloadType, SerializerOptions)
                ?? throw new InvalidOperationException($"Module event payload '{message.EventType}' deserialized to null.");

            var context = new ModuleEventContext(message.Id, message.EventType, message.OccurredAtUtc);
            foreach (var handler in _handlers.Where(handler => handler.PayloadType == payloadType))
                await DispatchToHandlerAsync(handler, payload, context, cancellationToken);

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

    private async Task DispatchToHandlerAsync(
        IModuleEventHandlerInvoker handler,
        object payload,
        ModuleEventContext context,
        CancellationToken cancellationToken)
    {
        var consumerName = handler.ConsumerName;
        if (string.IsNullOrWhiteSpace(consumerName))
            throw new InvalidOperationException($"Module event handler '{handler.GetType().FullName}' must provide a consumer name.");

        if (await _dbContext.InboxMessages.AnyAsync(
            inbox => inbox.ConsumerName == consumerName && inbox.MessageId == context.MessageId,
            cancellationToken))
        {
            return;
        }

        await handler.HandleAsync(payload, context, cancellationToken);
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
