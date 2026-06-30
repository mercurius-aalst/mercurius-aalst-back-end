namespace Platform.Eventing;

internal sealed class ModuleEventHandlerInvoker<TPayload, THandler> : IModuleEventHandlerInvoker
    where TPayload : notnull
    where THandler : class, IModuleEventHandler<TPayload>
{
    private readonly THandler _handler;

    public ModuleEventHandlerInvoker(THandler handler)
    {
        _handler = handler;
    }

    public Type PayloadType => typeof(TPayload);

    public string ConsumerName => _handler.ConsumerName;

    public Task HandleAsync(object payload, ModuleEventContext context, CancellationToken cancellationToken)
    {
        if (payload is not TPayload typedPayload)
            throw new InvalidOperationException($"Module event payload must be assignable to '{typeof(TPayload).FullName}'.");

        return _handler.HandleAsync(typedPayload, context, cancellationToken);
    }
}
