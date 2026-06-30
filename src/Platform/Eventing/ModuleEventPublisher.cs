using System.Text.Json;
using Platform.Eventing.Persistence;

namespace Platform.Eventing;

public sealed class ModuleEventPublisher : IModuleEventPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IModuleEventDbContext _dbContext;
    private readonly ModuleEventTypeRegistry _eventTypes;

    public ModuleEventPublisher(IModuleEventDbContext dbContext, ModuleEventTypeRegistry eventTypes)
    {
        _dbContext = dbContext;
        _eventTypes = eventTypes;
    }

    public Guid Publish<TPayload>(TPayload payload, DateTime? occurredAtUtc = null)
        where TPayload : notnull
    {
        var payloadType = payload.GetType();
        var eventType = _eventTypes.GetEventTypeName(payloadType);
        var messageId = Guid.NewGuid();

        _dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = messageId,
            EventType = eventType,
            Payload = JsonSerializer.Serialize(payload, payloadType, SerializerOptions),
            OccurredAtUtc = occurredAtUtc ?? DateTime.UtcNow
        });

        return messageId;
    }
}
