namespace Platform.Eventing;

internal interface IModuleEventHandlerInvoker
{
    Type PayloadType { get; }
    string ConsumerName { get; }

    Task HandleAsync(object payload, ModuleEventContext context, CancellationToken cancellationToken);
}
