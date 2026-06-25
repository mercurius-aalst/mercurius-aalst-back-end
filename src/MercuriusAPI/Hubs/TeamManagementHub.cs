using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.Services.TeamServices;
using Mercurius.Modules.Shared;
using Mercurius.Modules.Teams.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Mercurius.LAN.API.Hubs;

[Authorize]
public class TeamManagementHub : Hub
{
    private readonly MercuriusDBContext _dbContext;
    private readonly ITeamRealtimeAuthorizer _teamRealtimeAuthorizer;

    public TeamManagementHub(
        MercuriusDBContext dbContext,
        ITeamRealtimeAuthorizer teamRealtimeAuthorizer)
    {
        _dbContext = dbContext;
        _teamRealtimeAuthorizer = teamRealtimeAuthorizer;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId.HasValue)
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                TeamRealtimeGroups.GetUserGroup(userId.Value),
                Context.ConnectionAborted);

        await base.OnConnectedAsync();
    }

    public async Task JoinTeam(Guid teamId)
    {
        var userId = await GetCurrentUserIdAsync();
        if (!userId.HasValue)
            throw new HubException("Current user profile was not found.");

        var canJoin = await _teamRealtimeAuthorizer.CanSubscribeToTeamAsync(
            new TeamId(teamId),
            new UserId(userId.Value),
            Context.ConnectionAborted);

        if (!canJoin)
            throw new HubException("You are not allowed to subscribe to this team.");

        await Groups.AddToGroupAsync(Context.ConnectionId, TeamRealtimeGroups.GetTeamGroup(teamId), Context.ConnectionAborted);
    }

    public Task LeaveTeam(Guid teamId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, TeamRealtimeGroups.GetTeamGroup(teamId), Context.ConnectionAborted);
    }

    private async Task<Guid?> GetCurrentUserIdAsync()
    {
        var auth0UserId = Context.User?.FindFirstValue("sub") ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(auth0UserId))
            return null;

        return await _dbContext.Users
            .Where(user => user.Auth0UserId == auth0UserId.Trim() && !user.IsDeleted)
            .Select(user => (Guid?)user.Id)
            .FirstOrDefaultAsync(Context.ConnectionAborted);
    }
}
