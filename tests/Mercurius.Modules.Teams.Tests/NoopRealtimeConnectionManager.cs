using Platform.Realtime;

namespace Mercurius.Modules.Teams.Tests;

internal sealed class NoopRealtimeConnectionManager : IRealtimeConnectionManager
{
    public Task ExecuteWithAccessGateAsync(
        Func<CancellationToken, Task> action,
        Guid? userId,
        string? groupName = null,
        string? connectionId = null,
        CancellationToken cancellationToken = default) => action(cancellationToken);

    public void RegisterConnection(Guid userId, string connectionId, string personalGroup)
    {
    }

    public void TrackGroup(string connectionId, string groupName)
    {
    }

    public void UntrackGroup(string connectionId, string groupName)
    {
    }

    public void UnregisterConnection(string connectionId)
    {
    }

    public Task RevokeUserFromGroupAsync(
        Guid userId,
        string groupName,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task RevokeGroupAsync(
        string groupName,
        CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task RevokeUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default) => Task.CompletedTask;
}
