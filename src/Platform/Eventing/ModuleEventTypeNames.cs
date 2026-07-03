namespace Platform.Eventing;

internal static class ModuleEventTypeNames
{
    public static string GetName(Type eventType)
    {
        // Internal module events use their CLR full name as the durable type key,
        // keeping event contracts free from platform attributes or registration.
        return eventType.FullName
            ?? throw new InvalidOperationException($"Module event type '{eventType.Name}' must have a full name.");
    }

    public static Type Resolve(string eventTypeName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var eventType = assembly.GetType(eventTypeName, throwOnError: false, ignoreCase: false);
            if (eventType is not null)
                return eventType;
        }

        throw new InvalidOperationException($"Module event type '{eventTypeName}' could not be resolved.");
    }
}
