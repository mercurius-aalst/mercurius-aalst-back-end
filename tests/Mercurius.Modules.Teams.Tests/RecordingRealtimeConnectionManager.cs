using Platform.Realtime;

namespace Mercurius.Modules.Teams.Tests;

internal sealed class RecordingRealtimeConnectionManager : IRealtimeConnectionManager
{
    public List<UserGroupRevocation> UserGroupRevocations { get; } = [];

    public List<GroupRevocation> GroupRevocations { get; } = [];

    public List<Guid> UserRevocations { get; } = [];

    public List<string>? OperationOrder { get; init; }

    public bool ThrowOnRevocation { get; init; }

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
        CancellationToken cancellationToken = default)
    {
        UserGroupRevocations.Add(new UserGroupRevocation(userId, groupName, cancellationToken));
        OperationOrder?.Add("RevokeUserFromGroup");
        return CompleteRevocation();
    }

    public Task RevokeGroupAsync(
        string groupName,
        CancellationToken cancellationToken = default)
    {
        GroupRevocations.Add(new GroupRevocation(groupName, cancellationToken));
        OperationOrder?.Add("RevokeGroup");
        return CompleteRevocation();
    }

    public Task RevokeUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        UserRevocations.Add(userId);
        OperationOrder?.Add("RevokeUser");
        return CompleteRevocation();
    }

    private Task CompleteRevocation() => ThrowOnRevocation
        ? Task.FromException(new InvalidOperationException("planned post-commit revocation failure"))
        : Task.CompletedTask;

    internal sealed record UserGroupRevocation(
        Guid UserId,
        string GroupName,
        CancellationToken CancellationToken);

    internal sealed record GroupRevocation(
        string GroupName,
        CancellationToken CancellationToken);
}
