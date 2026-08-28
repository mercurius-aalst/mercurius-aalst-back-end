using Microsoft.AspNetCore.SignalR;

namespace Platform.Realtime;

internal sealed class SignalRRealtimeConnectionManager<THub> : IRealtimeConnectionManager
    where THub : Hub
{
    private readonly object _accessGatesLock = new();
    private readonly Dictionary<string, AccessGateEntry> _accessGates = new(StringComparer.Ordinal);
    private readonly object _stateLock = new();
    private readonly Dictionary<Guid, HashSet<string>> _connectionsByUser = [];
    private readonly Dictionary<string, ConnectionState> _connections = new(StringComparer.Ordinal);
    private readonly IHubContext<THub> _hubContext;

    public SignalRRealtimeConnectionManager(IHubContext<THub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task ExecuteWithAccessGateAsync(
        Func<CancellationToken, Task> action,
        Guid? userId,
        string? groupName = null,
        string? connectionId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        var keys = GetAccessKeys(userId, groupName, connectionId);
        var retainedGates = keys
            .Select(RetainAccessGate)
            .ToArray();
        var acquiredGates = new List<AccessGateEntry>(retainedGates.Length);
        try
        {
            foreach (var gate in retainedGates)
            {
                await gate.Semaphore.WaitAsync(cancellationToken);
                acquiredGates.Add(gate);
            }

            await action(cancellationToken);
        }
        finally
        {
            for (var index = acquiredGates.Count - 1; index >= 0; index--)
                acquiredGates[index].Semaphore.Release();

            foreach (var pair in keys.Zip(retainedGates))
                ReleaseAccessGate(pair.First, pair.Second);
        }
    }

    public void RegisterConnection(Guid userId, string connectionId, string personalGroup)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(personalGroup);

        lock (_stateLock)
        {
            UnregisterConnectionCore(connectionId);

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
    }

    public void TrackGroup(string connectionId, string groupName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);

        lock (_stateLock)
        {
            if (!_connections.TryGetValue(connectionId, out var connection))
                throw new InvalidOperationException($"Realtime connection '{connectionId}' is not registered.");

            connection.Groups.Add(groupName);
        }
    }

    public void UntrackGroup(string connectionId, string groupName)
    {
        lock (_stateLock)
        {
            if (_connections.TryGetValue(connectionId, out var connection))
                connection.Groups.Remove(groupName);
        }
    }

    public void UnregisterConnection(string connectionId)
    {
        lock (_stateLock)
            UnregisterConnectionCore(connectionId);
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
                foreach (var connection in SnapshotUserConnections(userId, groupName))
                {
                    await _hubContext.Groups.RemoveFromGroupAsync(
                        connection.ConnectionId,
                        groupName,
                        innerCancellationToken);
                    UntrackGroupIfCurrent(connection, groupName);
                }
            },
            userId: userId,
            groupName: groupName,
            cancellationToken: cancellationToken);
    }

    public Task RevokeGroupAsync(
        string groupName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);

        return ExecuteWithAccessGateAsync(
            async innerCancellationToken =>
            {
                foreach (var connection in SnapshotGroupConnections(groupName))
                {
                    await _hubContext.Groups.RemoveFromGroupAsync(
                        connection.ConnectionId,
                        groupName,
                        innerCancellationToken);
                    UntrackGroupIfCurrent(connection, groupName);
                }
            },
            userId: null,
            groupName: groupName,
            cancellationToken: cancellationToken);
    }

    public Task RevokeUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return ExecuteWithAccessGateAsync(
            async innerCancellationToken =>
            {
                foreach (var connection in SnapshotUserConnections(userId))
                {
                    foreach (var groupName in connection.Groups)
                    {
                        await _hubContext.Groups.RemoveFromGroupAsync(
                            connection.ConnectionId,
                            groupName,
                            innerCancellationToken);
                        UntrackGroupIfCurrent(connection, groupName);
                    }
                }
            },
            userId: userId,
            cancellationToken: cancellationToken);
    }

    private string[] GetAccessKeys(Guid? userId, string? groupName, string? connectionId)
    {
        if (groupName is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(groupName);

        var effectiveUserId = userId;
        if (!effectiveUserId.HasValue && connectionId is not null)
        {
            lock (_stateLock)
            {
                if (_connections.TryGetValue(connectionId, out var connection))
                    effectiveUserId = connection.UserId;
            }
        }

        var keys = new List<string>(3);
        if (effectiveUserId.HasValue)
            keys.Add($"user:{effectiveUserId.Value:N}");
        if (!string.IsNullOrWhiteSpace(groupName))
            keys.Add($"group:{groupName}");
        if (!string.IsNullOrWhiteSpace(connectionId))
            keys.Add($"connection:{connectionId}");

        keys.Sort(StringComparer.Ordinal);
        if (keys.Count == 0)
            throw new ArgumentException("At least one realtime access scope is required.");

        return keys.ToArray();
    }

    private AccessGateEntry RetainAccessGate(string key)
    {
        lock (_accessGatesLock)
        {
            if (!_accessGates.TryGetValue(key, out var entry))
            {
                entry = new AccessGateEntry();
                _accessGates.Add(key, entry);
            }

            entry.ReferenceCount++;
            return entry;
        }
    }

    private void ReleaseAccessGate(string key, AccessGateEntry entry)
    {
        lock (_accessGatesLock)
        {
            entry.ReferenceCount--;
            if (entry.ReferenceCount == 0 && _accessGates.Remove(key))
                entry.Semaphore.Dispose();
        }
    }

    private ConnectionSnapshot[] SnapshotUserConnections(
        Guid userId,
        string? groupName = null)
    {
        lock (_stateLock)
        {
            if (!_connectionsByUser.TryGetValue(userId, out var connectionIds))
                return [];

            return connectionIds
                .OrderBy(connectionId => connectionId, StringComparer.Ordinal)
                .Where(connectionId => _connections.ContainsKey(connectionId))
                .Select(connectionId => (ConnectionId: connectionId, State: _connections[connectionId]))
                .Where(connection => groupName is null || connection.State.Groups.Contains(groupName))
                .Select(connection => new ConnectionSnapshot(
                    connection.ConnectionId,
                    connection.State,
                    connection.State.Groups.ToArray()))
                .ToArray();
        }
    }

    private ConnectionSnapshot[] SnapshotGroupConnections(string groupName)
    {
        lock (_stateLock)
        {
            return _connections
                .Where(pair => pair.Value.Groups.Contains(groupName))
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new ConnectionSnapshot(
                    pair.Key,
                    pair.Value,
                    [groupName]))
                .ToArray();
        }
    }

    private void UntrackGroupIfCurrent(ConnectionSnapshot connection, string groupName)
    {
        lock (_stateLock)
        {
            if (_connections.TryGetValue(connection.ConnectionId, out var current) &&
                ReferenceEquals(current, connection.State))
            {
                current.Groups.Remove(groupName);
            }
        }
    }

    private void UnregisterConnectionCore(string connectionId)
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

    private sealed class AccessGateEntry
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);

        public int ReferenceCount { get; set; }
    }

    private sealed record ConnectionSnapshot(
        string ConnectionId,
        ConnectionState State,
        IReadOnlyList<string> Groups);

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
