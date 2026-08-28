namespace Platform.Eventing;

public interface IModuleEventPublisher
{
    Guid Publish<TPayload>(TPayload payload, DateTime? occurredAtUtc = null)
        where TPayload : notnull;
}
