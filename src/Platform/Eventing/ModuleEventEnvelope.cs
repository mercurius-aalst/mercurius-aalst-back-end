namespace Platform.Eventing;

public sealed record ModuleEventEnvelope<TPayload>(
    Guid MessageId,
    string EventType,
    TPayload Payload,
    DateTime OccurredAtUtc)
    where TPayload : notnull;
