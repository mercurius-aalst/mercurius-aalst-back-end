using System.Reflection;
using System.Security.Claims;
using Mercurius.LAN.API.Hubs;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace Mercurius.Api.Tests;

public class TeamManagementHubInvocationRateLimitFilterTests
{
    [Fact]
    public async Task InvokeMethodAsync_SharesOneWindowAcrossConnectionsAndSubscriptionMethods()
    {
        using var filter = new TeamManagementHubInvocationRateLimitFilter();
        var nextCalls = 0;

        for (var attempt = 0; attempt < TeamManagementHubInvocationRateLimitFilter.PermitLimit; attempt++)
        {
            var methodName = attempt % 2 == 0 ? nameof(TestHub.JoinTeam) : nameof(TestHub.LeaveTeam);
            var connectionId = attempt % 2 == 0 ? "first-connection" : "second-connection";

            await filter.InvokeMethodAsync(
                CreateInvocationContext("auth0|shared-user", connectionId, methodName),
                _ =>
                {
                    nextCalls++;
                    return ValueTask.FromResult<object?>(null);
                });
        }

        var rejectedNextCalled = false;
        var exception = await Assert.ThrowsAsync<HubException>(async () =>
            await filter.InvokeMethodAsync(
                CreateInvocationContext("auth0|shared-user", "third-connection", nameof(TestHub.JoinTeam)),
                _ =>
                {
                    rejectedNextCalled = true;
                    return ValueTask.FromResult<object?>(null);
                }));

        Assert.Equal(TeamManagementHubInvocationRateLimitFilter.PermitLimit, nextCalls);
        Assert.False(rejectedNextCalled);
        Assert.StartsWith("Too many team subscription requests. Retry after ", exception.Message);
        Assert.EndsWith(" seconds.", exception.Message);
    }

    [Fact]
    public async Task InvokeMethodAsync_UsesIndependentWindowsForDifferentSubjects()
    {
        using var filter = new TeamManagementHubInvocationRateLimitFilter();
        await ConsumeSubscriptionWindowAsync(filter, "auth0|first-user");
        var secondUserNextCalled = false;

        await filter.InvokeMethodAsync(
            CreateInvocationContext("auth0|second-user", "connection", nameof(TestHub.JoinTeam)),
            _ =>
            {
                secondUserNextCalled = true;
                return ValueTask.FromResult<object?>(null);
            });

        Assert.True(secondUserNextCalled);
    }

    [Fact]
    public async Task InvokeMethodAsync_DoesNotLimitUnrelatedHubMethods()
    {
        using var filter = new TeamManagementHubInvocationRateLimitFilter();
        await ConsumeSubscriptionWindowAsync(filter, "auth0|user");
        var nextCalled = false;

        await filter.InvokeMethodAsync(
            CreateInvocationContext("auth0|user", "connection", nameof(TestHub.ReceiveNotification)),
            _ =>
            {
                nextCalled = true;
                return ValueTask.FromResult<object?>(null);
            });

        Assert.True(nextCalled);
    }

    private static async Task ConsumeSubscriptionWindowAsync(
        TeamManagementHubInvocationRateLimitFilter filter,
        string subject)
    {
        for (var attempt = 0; attempt < TeamManagementHubInvocationRateLimitFilter.PermitLimit; attempt++)
        {
            await filter.InvokeMethodAsync(
                CreateInvocationContext(subject, "connection", nameof(TestHub.JoinTeam)),
                _ => ValueTask.FromResult<object?>(null));
        }
    }

    private static HubInvocationContext CreateInvocationContext(
        string subject,
        string connectionId,
        string methodName)
    {
        var hub = new TestHub();
        var hubMethod = typeof(TestHub).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public)!;
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", subject)], "Bearer"));

        return new HubInvocationContext(
            new TestHubCallerContext(connectionId, user),
            new ServiceCollection().BuildServiceProvider(),
            hub,
            hubMethod,
            []);
    }

    private sealed class TestHub : Hub
    {
        public Task JoinTeam(Guid teamId) => Task.CompletedTask;

        public Task LeaveTeam(Guid teamId) => Task.CompletedTask;

        public Task ReceiveNotification() => Task.CompletedTask;
    }

    private sealed class TestHubCallerContext(
        string connectionId,
        ClaimsPrincipal user) : HubCallerContext
    {
        public override string ConnectionId { get; } = connectionId;

        public override string? UserIdentifier => null;

        public override ClaimsPrincipal User { get; } = user;

        public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();

        public override IFeatureCollection Features { get; } = new FeatureCollection();

        public override CancellationToken ConnectionAborted => CancellationToken.None;

        public override void Abort()
        {
        }
    }
}
