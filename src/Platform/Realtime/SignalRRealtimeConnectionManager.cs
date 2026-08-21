using Microsoft.AspNetCore.SignalR;

namespace Platform.Realtime;

internal sealed class SignalRRealtimeConnectionManager<THub> : IRealtimeConnectionManager
    where THub : Hub
{
    private readonly SemaphoreSlim _accessGate = new(1, 1);
    private readonly Dictionary<Guid, HashSet<string>> _connectionsByUser = [];
    private readonly Dictionary<string, ConnectionState> _connections = new(StringComparer.Ordinal);
    private readonly IHubContext<THub> _hubContext;

    public SignalRRealtimeConnectionManager(IHubContext<THub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task ExecuteWithAccessGateAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        await _accessGate.WaitAsync(cancellationToken);
        try
        {
            await action(cancellationToken);
        }
        finally
        {
            _accessGate.Release();
        }
    }

    public void RegisterConnection(Guid userId, string connectionId, string personalGroup)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(personalGroup);

        UnregisterConnection(connectionId);

        var connection = new ConnectionState(userId);
        connection.Groups.Add(personalGroup);
        _connections.Add(connectionId, connection);

        if (!_connectionsByUser.TryGetValue(userId, out var connectionIds))
        {
            connectionIds = new HashSet<string>(StringComparer.Ordinal);
            _connectionsByUser.Add(userId, connectionIds);
        }

        connectionIds.Add(connectionId);
    }

    public void TrackGroup(string connectionId, string groupName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);

        if (!_connections.TryGetValue(connectionId, out var connection))
            throw new InvalidOperationException($"Realtime connection '{connectionId}' is not registered.");

        connection.Groups.Add(groupName);
    }

    public void UntrackGroup(string connectionId, string groupName)
    {
        if (_connections.TryGetValue(connectionId, out var connection))
            connection.Groups.Remove(groupName);
    }

    public void UnregisterConnection(string connectionId)
    {
        if (!_connections.Remove(connectionId, out var connection) ||
            !_connectionsByUser.TryGetValue(connection.UserId, out var connectionIds))
        {
            return;
        }

        connectionIds.Remove(connectionId);
        if (connectionIds.Count == 0)
            _connectionsByUser.Remove(connection.UserId);
    }

    public Task RevokeUserFromGroupAsync(
        Guid userId,
        string groupName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);

        return ExecuteWithAccessGateAsync(
            async innerCancellationToken =>
            {
                if (!_connectionsByUser.TryGetValue(userId, out var connectionIds))
                    return;

                foreach (var connectionId in connectionIds.ToArray())
                {
                    if (!_connections.TryGetValue(connectionId, out var connection) ||
                        !connection.Groups.Contains(groupName))
                    {
                        continue;
                    }

                    await _hubContext.Groups.RemoveFromGroupAsync(
                        connectionId,
                        groupName,
                        innerCancellationToken);
                    connection.Groups.Remove(groupName);
                }
            },
            cancellationToken);
    }

    public Task RevokeGroupAsync(
        string groupName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);

        return ExecuteWithAccessGateAsync(
            async innerCancellationToken =>
            {
                var connectionIds = _connections
                    .Where(pair => pair.Value.Groups.Contains(groupName))
                    .Select(pair => pair.Key)
                    .ToArray();

                foreach (var connectionId in connectionIds)
                {
                    await _hubContext.Groups.RemoveFromGroupAsync(
                        connectionId,
                        groupName,
                        innerCancellationToken);
                    _connections[connectionId].Groups.Remove(groupName);
                }
            },
            cancellationToken);
    }

    public Task RevokeUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteWithAccessGateAsync(
            async innerCancellationToken =>
            {
                if (!_connectionsByUser.TryGetValue(userId, out var connectionIds))
                    return;

                foreach (var connectionId in connectionIds.ToArray())
                {
                    if (!_connections.TryGetValue(connectionId, out var connection))
                        continue;

                    foreach (var groupName in connection.Groups.ToArray())
                    {
                        await _hubContext.Groups.RemoveFromGroupAsync(
                            connectionId,
                            groupName,
                            innerCancellationToken);
                        connection.Groups.Remove(groupName);
                    }
                }
            },
            cancellationToken);
    }

    private sealed class ConnectionState
    {
        public ConnectionState(Guid userId)
        {
            UserId = userId;
        }

        public Guid UserId { get; }

        public HashSet<string> Groups { get; } = new(StringComparer.Ordinal);
    }
}
