using Platform.Realtime;

namespace Mercurius.Modules.Teams.Services;

public interface ITeamEventPublisher
{
    Task InviteChangedAsync(Guid teamId, Guid inviteId, Guid affectedUserId, string status);
    Task RosterConfirmationChangedAsync(Guid teamId, Guid rosterMemberId, Guid affectedUserId, string status);
    Task MembershipChangedAsync(Guid teamId, Guid affectedUserId, string action);
    Task CaptainTransferredAsync(Guid teamId, Guid newCaptainUserId);
}

internal sealed class RealtimeTeamEventPublisher : ITeamEventPublisher
{
    private readonly IRealtimePublisher _realtimePublisher;

    public RealtimeTeamEventPublisher(IRealtimePublisher realtimePublisher)
    {
        _realtimePublisher = realtimePublisher;
    }

    public Task InviteChangedAsync(Guid teamId, Guid inviteId, Guid affectedUserId, string status)
    {
        return _realtimePublisher.PublishAsync(new RealtimePublishRequest<TeamInviteChangedEvent>(
            "TeamInviteChanged",
            new TeamInviteChangedEvent(teamId, inviteId, affectedUserId, status),
            [TeamRealtimeGroups.GetUserGroup(affectedUserId)]));
    }

    public Task MembershipChangedAsync(Guid teamId, Guid affectedUserId, string action)
    {
        return _realtimePublisher.PublishAsync(new RealtimePublishRequest<TeamMembershipChangedEvent>(
            "TeamMembershipChanged",
            new TeamMembershipChangedEvent(teamId, affectedUserId, action),
            [TeamRealtimeGroups.GetTeamGroup(teamId)]));
    }

    public Task RosterConfirmationChangedAsync(Guid teamId, Guid rosterMemberId, Guid affectedUserId, string status)
    {
        return _realtimePublisher.PublishAsync(new RealtimePublishRequest<TournamentRosterConfirmationChangedEvent>(
            "TournamentRosterConfirmationChanged",
            new TournamentRosterConfirmationChangedEvent(teamId, rosterMemberId, affectedUserId, status),
            [TeamRealtimeGroups.GetUserGroup(affectedUserId), TeamRealtimeGroups.GetTeamGroup(teamId)]));
    }

    public Task CaptainTransferredAsync(Guid teamId, Guid newCaptainUserId)
    {
        return _realtimePublisher.PublishAsync(new RealtimePublishRequest<TeamCaptainTransferredEvent>(
            "TeamCaptainTransferred",
            new TeamCaptainTransferredEvent(teamId, newCaptainUserId),
            [TeamRealtimeGroups.GetTeamGroup(teamId)]));
    }
}

internal sealed class NullTeamEventPublisher : ITeamEventPublisher
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
