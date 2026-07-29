namespace Mercurius.Modules.Teams.DTOs;

internal sealed class CurrentUserTeamSummaryResponseDTO
{
    public IReadOnlyList<TeamManagementSummaryResponseDTO> CaptainedTeams { get; set; } = [];
    public IReadOnlyList<TeamManagementSummaryResponseDTO> MemberTeams { get; set; } = [];
    public IReadOnlyList<TeamInviteSummaryResponseDTO> ReceivedPendingInvites { get; set; } = [];
    public IReadOnlyList<TeamInviteSummaryResponseDTO> SentPendingInvites { get; set; } = [];
}
