using System.Text.Json;
using Platform.Eventing.Persistence;

namespace Platform.Eventing;

public sealed class ModuleEventPublisher : IModuleEventPublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IModuleEventDbContext _dbContext;

    public ModuleEventPublisher(IModuleEventDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Guid Publish<TPayload>(TPayload payload, DateTime? occurredAtUtc = null)
        where TPayload : notnull
    {
        var payloadType = payload.GetType();
        var messageId = Guid.NewGuid();
        var occurredAt = occurredAtUtc ?? DateTime.UtcNow;

        _dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = messageId,
            EventType = ModuleEventTypeNames.GetName(payloadType),
            Payload = JsonSerializer.Serialize(payload, payloadType, SerializerOptions),
            OccurredAtUtc = occurredAt,
            NextAttemptAtUtc = occurredAt
        });

        return messageId;
    }
}
