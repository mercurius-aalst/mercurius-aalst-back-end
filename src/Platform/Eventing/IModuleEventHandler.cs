namespace Platform.Eventing;

public interface IModuleEventHandler<in TPayload>
    where TPayload : notnull
{
    string ConsumerName { get; }

    Task HandleAsync(TPayload payload, ModuleEventContext context, CancellationToken cancellationToken = default);
}
