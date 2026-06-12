using Mercurius.LAN.API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Mercurius.LAN.API.Services.TeamServices;

public interface ITeamEventPublisher
{
    Task InviteChangedAsync(Guid teamId, Guid inviteId, Guid affectedUserId, string status);
    Task RosterConfirmationChangedAsync(Guid teamId, Guid rosterMemberId, Guid affectedUserId, string status);
    Task MembershipChangedAsync(Guid teamId, Guid affectedUserId, string action);
    Task CaptainTransferredAsync(Guid teamId, Guid newCaptainUserId);
}

public class SignalRTeamEventPublisher : ITeamEventPublisher
{
    private readonly IHubContext<TeamManagementHub> _hubContext;

    public SignalRTeamEventPublisher(IHubContext<TeamManagementHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task InviteChangedAsync(Guid teamId, Guid inviteId, Guid affectedUserId, string status)
    {
        return _hubContext.Clients.Group(GetUserGroup(affectedUserId))
            .SendAsync("TeamInviteChanged", new TeamInviteChangedEvent(teamId, inviteId, affectedUserId, status));
    }

    public Task MembershipChangedAsync(Guid teamId, Guid affectedUserId, string action)
    {
        return _hubContext.Clients.Group(GetTeamGroup(teamId))
            .SendAsync("TeamMembershipChanged", new TeamMembershipChangedEvent(teamId, affectedUserId, action));
    }

    public Task RosterConfirmationChangedAsync(Guid teamId, Guid rosterMemberId, Guid affectedUserId, string status)
    {
        return _hubContext.Clients.Groups([GetUserGroup(affectedUserId), GetTeamGroup(teamId)])
            .SendAsync(
                "TournamentRosterConfirmationChanged",
                new TournamentRosterConfirmationChangedEvent(teamId, rosterMemberId, affectedUserId, status));
    }

    public Task CaptainTransferredAsync(Guid teamId, Guid newCaptainUserId)
    {
        return _hubContext.Clients.Group(GetTeamGroup(teamId))
            .SendAsync("TeamCaptainTransferred", new TeamCaptainTransferredEvent(teamId, newCaptainUserId));
    }

    public static string GetTeamGroup(Guid teamId) => $"team:{teamId:N}";
    public static string GetUserGroup(Guid userId) => $"user:{userId:N}";
}

public class NullTeamEventPublisher : ITeamEventPublisher
{
    public Task InviteChangedAsync(Guid teamId, Guid inviteId, Guid affectedUserId, string status) => Task.CompletedTask;
    public Task RosterConfirmationChangedAsync(Guid teamId, Guid rosterMemberId, Guid affectedUserId, string status) => Task.CompletedTask;
    public Task MembershipChangedAsync(Guid teamId, Guid affectedUserId, string action) => Task.CompletedTask;
    public Task CaptainTransferredAsync(Guid teamId, Guid newCaptainUserId) => Task.CompletedTask;
}

public record TeamInviteChangedEvent(Guid TeamId, Guid InviteId, Guid UserId, string Status);
public record TournamentRosterConfirmationChangedEvent(Guid TeamId, Guid RosterMemberId, Guid UserId, string Status); 
public record TeamMembershipChangedEvent(Guid TeamId, Guid UserId, string Action);
public record TeamCaptainTransferredEvent(Guid TeamId, Guid NewCaptainUserId);
