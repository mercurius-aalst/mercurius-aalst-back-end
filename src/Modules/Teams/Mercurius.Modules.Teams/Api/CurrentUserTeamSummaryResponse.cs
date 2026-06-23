namespace Mercurius.Modules.Teams.Api;

public sealed class CurrentUserTeamSummaryResponse
{
    public IReadOnlyList<TeamManagementSummaryResponse> CaptainedTeams { get; set; } = [];
    public IReadOnlyList<TeamManagementSummaryResponse> MemberTeams { get; set; } = [];
    public IReadOnlyList<TeamInviteSummaryResponse> ReceivedPendingInvites { get; set; } = [];
    public IReadOnlyList<TeamInviteSummaryResponse> SentPendingInvites { get; set; } = [];
}
