namespace Platform.Eventing;

public sealed record ModuleEventContext(
    Guid MessageId,
    string EventType,
    DateTime OccurredAtUtc);
