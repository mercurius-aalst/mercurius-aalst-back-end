namespace Platform.Eventing;

public static class ProjectionVersionGuard
{
    public static bool IsStale(long incomingVersion, long storedVersion)
    {
        return incomingVersion < storedVersion;
    }
}
