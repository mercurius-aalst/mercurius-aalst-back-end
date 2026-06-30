namespace Platform.Eventing;

public sealed class ModuleEventingOptions
{
    private readonly Dictionary<Type, string> _eventTypes = [];

    public IReadOnlyDictionary<Type, string> EventTypes => _eventTypes;

    public ModuleEventingOptions RegisterEvent<TPayload>(string? eventType = null)
        where TPayload : notnull
    {
        var payloadType = typeof(TPayload);
        _eventTypes[payloadType] = string.IsNullOrWhiteSpace(eventType)
            ? payloadType.FullName ?? payloadType.Name
            : eventType.Trim();

        return this;
    }
}
