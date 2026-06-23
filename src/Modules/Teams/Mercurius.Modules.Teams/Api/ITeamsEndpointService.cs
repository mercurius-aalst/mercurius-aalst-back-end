using Microsoft.AspNetCore.Http;

namespace Mercurius.Modules.Teams.Api;

public interface ITeamsEndpointService
{
    Task<IEnumerable<TeamResponse>> GetAllTeamsAsync(CancellationToken cancellationToken = default);
    Task<TeamResponse> GetTeamByIdAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<TeamManagementSummaryResponse> CreateCurrentUserTeamAsync(string auth0UserId, CreateTeamRequest request);
    Task<CurrentUserTeamSummaryResponse> GetCurrentUserTeamSummaryAsync(string auth0UserId);
    Task<IEnumerable<TeamInviteSummaryResponse>> GetCurrentUserInvitesAsync(string auth0UserId);
    Task<IEnumerable<TeamInviteSummaryResponse>> GetCurrentUserSentInvitesAsync(string auth0UserId);
    Task<TeamManagementSummaryResponse> LeaveTeamAsync(string auth0UserId, Guid teamId);
    Task<TeamManagementSummaryResponse> RemoveMemberAsync(string auth0UserId, Guid teamId, Guid userId);
    Task DeleteTeamAsync(string auth0UserId, Guid teamId);
    Task<TeamInviteResponse> InviteUserAsync(string auth0UserId, Guid teamId, Guid userId);
    Task<TeamInviteResponse> CancelInviteAsync(string auth0UserId, Guid teamId, Guid inviteId);
    Task<TeamInviteResponse> RespondToInviteAsync(string auth0UserId, Guid inviteId, bool accept);
    Task<TeamManagementSummaryResponse> TransferCaptainAsync(string auth0UserId, Guid teamId, Guid newCaptainUserId);
    Task<TeamLogoResponse> UploadTeamLogoAsync(string auth0UserId, Guid teamId, IFormFile logo);
    Task<TeamLogoResponse> RemoveTeamLogoAsync(string auth0UserId, Guid teamId);
    Task<PublicTeamProfileResponse> GetPublicTeamProfileAsync(string teamName);
}
