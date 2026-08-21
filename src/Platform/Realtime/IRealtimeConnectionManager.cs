namespace Platform.Realtime;

public interface IRealtimeConnectionManager
{
    Task ExecuteWithAccessGateAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default);

    void RegisterConnection(Guid userId, string connectionId, string personalGroup);

    void TrackGroup(string connectionId, string groupName);

    void UntrackGroup(string connectionId, string groupName);

    void UnregisterConnection(string connectionId);

    Task RevokeUserFromGroupAsync(
        Guid userId,
        string groupName,
        CancellationToken cancellationToken = default);

    Task RevokeGroupAsync(
        string groupName,
        CancellationToken cancellationToken = default);

    Task RevokeUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
