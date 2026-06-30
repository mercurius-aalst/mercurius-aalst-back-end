namespace Platform.Eventing;

public sealed class ModuleEventTypeRegistry
{
    private readonly Dictionary<Type, string> _eventTypeNames;
    private readonly Dictionary<string, Type> _payloadTypes;

    public ModuleEventTypeRegistry(ModuleEventingOptions options)
    {
        _eventTypeNames = options.EventTypes.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            EqualityComparer<Type>.Default);

        _payloadTypes = new Dictionary<string, Type>(StringComparer.Ordinal);
        foreach (var (payloadType, eventTypeName) in options.EventTypes)
        {
            if (!_payloadTypes.TryAdd(eventTypeName, payloadType))
                throw new InvalidOperationException($"Duplicate module event type name '{eventTypeName}'.");
        }
    }

    public string GetEventTypeName(Type payloadType)
    {
        if (_eventTypeNames.TryGetValue(payloadType, out var eventTypeName))
            return eventTypeName;

        throw new InvalidOperationException($"Module event payload type '{payloadType.FullName}' is not registered.");
    }

    public Type GetPayloadType(string eventTypeName)
    {
        if (_payloadTypes.TryGetValue(eventTypeName, out var payloadType))
            return payloadType;

        throw new InvalidOperationException($"Module event type '{eventTypeName}' is not registered.");
    }
}
