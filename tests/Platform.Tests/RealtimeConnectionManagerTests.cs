using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Platform.Extensions;
using Platform.Realtime;

namespace Platform.Tests;

public class RealtimeConnectionManagerTests
{
    [Fact]
    public async Task Revocation_CoversMultipleConnectionsAndIsolatesUsersAndGroups()
    {
        var (provider, manager, groups) = CreateManager();
        await using var disposableProvider = provider;
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();
        const string firstTeamGroup = "team:first";
        const string secondTeamGroup = "team:second";

        await manager.ExecuteWithAccessGateAsync(_ =>
        {
            manager.RegisterConnection(firstUserId, "first-connection", "user:first");
            manager.TrackGroup("first-connection", firstTeamGroup);
            manager.RegisterConnection(firstUserId, "second-connection", "user:first");
            manager.TrackGroup("second-connection", firstTeamGroup);
            manager.TrackGroup("second-connection", secondTeamGroup);
            manager.RegisterConnection(secondUserId, "other-connection", "user:other");
            manager.TrackGroup("other-connection", firstTeamGroup);
            return Task.CompletedTask;
        }, userId: null, connectionId: "first-connection");

        await manager.RevokeUserFromGroupAsync(firstUserId, firstTeamGroup);
        Assert.Equal(
            [new GroupRemoval("first-connection", firstTeamGroup), new GroupRemoval("second-connection", firstTeamGroup)],
            groups.Removals);

        await manager.RevokeGroupAsync(firstTeamGroup);
        Assert.Equal(new GroupRemoval("other-connection", firstTeamGroup), groups.Removals[^1]);

        await manager.RevokeUserAsync(firstUserId);
        Assert.Contains(new GroupRemoval("first-connection", "user:first"), groups.Removals);
        Assert.Contains(new GroupRemoval("second-connection", "user:first"), groups.Removals);
        Assert.Contains(new GroupRemoval("second-connection", secondTeamGroup), groups.Removals);
        Assert.DoesNotContain(new GroupRemoval("other-connection", "user:other"), groups.Removals);
    }

    [Fact]
    public async Task LeaveAndDisconnectCleanup_RemoveTrackedState()
    {
        var (provider, manager, groups) = CreateManager();
        await using var disposableProvider = provider;
        var userId = Guid.NewGuid();
        const string teamGroup = "team:one";

        await manager.ExecuteWithAccessGateAsync(_ =>
        {
            manager.RegisterConnection(userId, "connection", "user:one");
            manager.TrackGroup("connection", teamGroup);
            manager.UntrackGroup("connection", teamGroup);
            return Task.CompletedTask;
        }, userId: null, connectionId: "connection");
        await manager.RevokeGroupAsync(teamGroup);
        Assert.Empty(groups.Removals);

        await manager.ExecuteWithAccessGateAsync(_ =>
        {
            manager.UnregisterConnection("connection");
            return Task.CompletedTask;
        }, userId: null, connectionId: "connection");
        await manager.RevokeUserAsync(userId);
        Assert.Empty(groups.Removals);
    }

    [Fact]
    public async Task JoinAndRevokeRace_LeavesNoTrackedTeamAccess()
    {
        var (provider, manager, groups) = CreateManager();
        await using var disposableProvider = provider;
        var userId = Guid.NewGuid();
        const string teamGroup = "team:race";
        var joinEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseJoin = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var joinTask = manager.ExecuteWithAccessGateAsync(async cancellationToken =>
        {
            manager.RegisterConnection(userId, "connection", "user:race");
            joinEntered.SetResult();
            await releaseJoin.Task.WaitAsync(cancellationToken);
            manager.TrackGroup("connection", teamGroup);
        }, userId: userId, groupName: teamGroup);

        await joinEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var revokeTask = manager.RevokeUserFromGroupAsync(userId, teamGroup);
        Assert.False(revokeTask.IsCompleted);

        releaseJoin.SetResult();
        await Task.WhenAll(joinTask, revokeTask).WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal([new GroupRemoval("connection", teamGroup)], groups.Removals);
        await manager.RevokeGroupAsync(teamGroup);
        Assert.Single(groups.Removals);
    }

    [Fact]
    public async Task UnrelatedAccessSubjects_RunConcurrently()
    {
        var (provider, manager, _) = CreateManager();
        await using var disposableProvider = provider;
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();

        var first = manager.ExecuteWithAccessGateAsync(
            async cancellationToken =>
            {
                firstEntered.SetResult();
                await release.Task.WaitAsync(cancellationToken);
            },
            userId: firstUserId,
            groupName: "team:first");
        var second = manager.ExecuteWithAccessGateAsync(
            async cancellationToken =>
            {
                secondEntered.SetResult();
                await release.Task.WaitAsync(cancellationToken);
            },
            userId: secondUserId,
            groupName: "team:second");

        await Task.WhenAll(firstEntered.Task, secondEntered.Task).WaitAsync(TimeSpan.FromSeconds(2));
        release.SetResult();
        await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task AccessGate_CleansUpAfterCancellationAndException()
    {
        var (provider, manager, _) = CreateManager();
        await using var disposableProvider = provider;
        var userId = Guid.NewGuid();
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => manager.ExecuteWithAccessGateAsync(
            _ => Task.CompletedTask,
            userId: userId,
            cancellationToken: cancellationTokenSource.Token));

        var completedAfterCancellation = false;
        await manager.ExecuteWithAccessGateAsync(
            _ =>
            {
                completedAfterCancellation = true;
                return Task.CompletedTask;
            },
            userId: userId);
        Assert.True(completedAfterCancellation);

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.ExecuteWithAccessGateAsync(
            _ => throw new InvalidOperationException("planned failure"),
            userId: null,
            groupName: "team:cleanup"));

        var completedAfterException = false;
        await manager.ExecuteWithAccessGateAsync(
            _ =>
            {
                completedAfterException = true;
                return Task.CompletedTask;
            },
            userId: null,
            groupName: "team:cleanup");
        Assert.True(completedAfterException);
    }

    private static (ServiceProvider Provider, IRealtimeConnectionManager Manager, RecordingGroupManager Groups) CreateManager()
    {
        var groups = new RecordingGroupManager();
        var hubContext = new RecordingHubContext(groups);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRealtimeNotificationServices<TestHub>();
        services.AddSingleton<IHubContext<TestHub>>(hubContext);
        var provider = services.BuildServiceProvider();

        return (provider, provider.GetRequiredService<IRealtimeConnectionManager>(), groups);
    }

    private sealed class TestHub : Hub;

    private sealed class RecordingHubContext(RecordingGroupManager groups) : IHubContext<TestHub>
    {
        public IHubClients Clients => throw new NotSupportedException();

        public IGroupManager Groups { get; } = groups;
    }

    private sealed class RecordingGroupManager : IGroupManager
    {
        public List<GroupRemoval> Removals { get; } = [];

        public Task AddToGroupAsync(
            string connectionId,
            string groupName,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RemoveFromGroupAsync(
            string connectionId,
            string groupName,
            CancellationToken cancellationToken = default)
        {
            Removals.Add(new GroupRemoval(connectionId, groupName));
            return Task.CompletedTask;
        }
    }

    private sealed record GroupRemoval(string ConnectionId, string GroupName);
}
