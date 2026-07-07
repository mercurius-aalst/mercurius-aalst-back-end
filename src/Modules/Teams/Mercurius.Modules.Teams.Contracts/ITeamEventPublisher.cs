namespace Mercurius.Modules.Teams.Contracts;

public interface ITeamEventPublisher
{
    Task InviteChangedAsync(Guid teamId, Guid inviteId, Guid affectedUserId, string status);

    Task RosterConfirmationChangedAsync(Guid teamId, Guid rosterMemberId, Guid affectedUserId, string status);

    Task MembershipChangedAsync(Guid teamId, Guid affectedUserId, string action);

    Task CaptainTransferredAsync(Guid teamId, Guid newCaptainUserId);
}
