using Mercurius.LAN.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Mercurius.LAN.API.Hubs;

[Authorize]
public class TeamManagementHub : Hub
{
    private readonly MercuriusDBContext _dbContext;

    public TeamManagementHub(MercuriusDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = await GetCurrentUserIdAsync();
        if (userId.HasValue)
            await Groups.AddToGroupAsync(Context.ConnectionId, Services.TeamServices.SignalRTeamEventPublisher.GetUserGroup(userId.Value));

        await base.OnConnectedAsync();
    }

    public async Task JoinTeam(Guid teamId)
    {
        var userId = await GetCurrentUserIdAsync();
        if (!userId.HasValue)
            throw new HubException("Current user profile was not found.");

        var canJoin = await _dbContext.Teams.AnyAsync(team =>
            team.Id == teamId &&
            (team.CaptainUserId == userId.Value || team.Members.Any(member => member.Id == userId.Value)));

        if (!canJoin)
            throw new HubException("You are not allowed to subscribe to this team.");

        await Groups.AddToGroupAsync(Context.ConnectionId, Services.TeamServices.SignalRTeamEventPublisher.GetTeamGroup(teamId));
    }

    public Task LeaveTeam(Guid teamId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, Services.TeamServices.SignalRTeamEventPublisher.GetTeamGroup(teamId));
    }

    private async Task<Guid?> GetCurrentUserIdAsync()
    {
        var auth0UserId = Context.User?.FindFirstValue("sub") ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(auth0UserId))
            return null;

        return await _dbContext.Users
            .Where(user => user.Auth0UserId == auth0UserId.Trim() && !user.IsDeleted)
            .Select(user => (Guid?)user.Id)
            .FirstOrDefaultAsync();
    }
}
