using Mercurius.LAN.API.Data;
using Mercurius.Modules.Shared;
using Mercurius.Modules.Teams.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Platform.Realtime;
using System.Security.Claims;

namespace Mercurius.LAN.API.Hubs;

[Authorize]
public class TeamManagementHub : Hub
{
    public const string Route = "/v1/lan/team-events";

    private readonly MercuriusDBContext _dbContext;
    private readonly IRealtimeConnectionManager _connectionManager;
    private readonly ITeamRealtimeAuthorizer _teamRealtimeAuthorizer;

    public TeamManagementHub(
        MercuriusDBContext dbContext,
        ITeamRealtimeAuthorizer teamRealtimeAuthorizer,
        IRealtimeConnectionManager connectionManager)
    {
        _dbContext = dbContext;
        _teamRealtimeAuthorizer = teamRealtimeAuthorizer;
        _connectionManager = connectionManager;
    }

    public override async Task OnConnectedAsync()
    {
        await _connectionManager.ExecuteWithAccessGateAsync(
            async cancellationToken =>
            {
                var userId = await GetCurrentUserIdAsync(cancellationToken);
                if (!userId.HasValue)
                    return;

                var personalGroup = TeamRealtimeGroups.GetUserGroup(userId.Value);
                await Groups.AddToGroupAsync(Context.ConnectionId, personalGroup, cancellationToken);
                _connectionManager.RegisterConnection(userId.Value, Context.ConnectionId, personalGroup);
            },
            Context.ConnectionAborted);

        await base.OnConnectedAsync();
    }

    public async Task JoinTeam(Guid teamId)
    {
        await _connectionManager.ExecuteWithAccessGateAsync(
            async cancellationToken =>
            {
                var userId = await GetCurrentUserIdAsync(cancellationToken);
                if (!userId.HasValue)
                    throw new HubException("Current user profile was not found.");

                var canJoin = await _teamRealtimeAuthorizer.CanSubscribeToTeamAsync(
                    new TeamId(teamId),
                    new UserId(userId.Value),
                    cancellationToken);

                if (!canJoin)
                    throw new HubException("You are not allowed to subscribe to this team.");

                var teamGroup = TeamRealtimeGroups.GetTeamGroup(teamId);
                await Groups.AddToGroupAsync(Context.ConnectionId, teamGroup, cancellationToken);
                _connectionManager.TrackGroup(Context.ConnectionId, teamGroup);
            },
            Context.ConnectionAborted);
    }

    public Task LeaveTeam(Guid teamId)
    {
        return _connectionManager.ExecuteWithAccessGateAsync(
            async cancellationToken =>
            {
                var teamGroup = TeamRealtimeGroups.GetTeamGroup(teamId);
                try
                {
                    await Groups.RemoveFromGroupAsync(Context.ConnectionId, teamGroup, cancellationToken);
                }
                finally
                {
                    _connectionManager.UntrackGroup(Context.ConnectionId, teamGroup);
                }
            },
            Context.ConnectionAborted);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            await _connectionManager.ExecuteWithAccessGateAsync(
                _ =>
                {
                    _connectionManager.UnregisterConnection(Context.ConnectionId);
                    return Task.CompletedTask;
                },
                CancellationToken.None);
        }
        finally
        {
            await base.OnDisconnectedAsync(exception);
        }
    }

    private async Task<Guid?> GetCurrentUserIdAsync(CancellationToken cancellationToken)
    {
        var auth0UserId = Context.User?.FindFirstValue("sub") ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(auth0UserId))
            return null;

        return await _dbContext.Users
            .Where(user => user.Auth0UserId == auth0UserId.Trim() && !user.IsDeleted)
            .Select(user => (Guid?)user.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
