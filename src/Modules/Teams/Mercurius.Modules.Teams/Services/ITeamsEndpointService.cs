using Microsoft.AspNetCore.Http;
using Mercurius.Modules.Teams.Models;

namespace Mercurius.Modules.Teams.Services;

public interface ITeamsEndpointService
{
    Task<IReadOnlyList<TeamResponse>> GetAllTeamsAsync(CancellationToken cancellationToken = default);
    Task<TeamResponse> GetTeamByIdAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<TeamManagementSummaryResponse> CreateCurrentUserTeamAsync(string auth0UserId, CreateTeamRequest request, CancellationToken cancellationToken = default);
    Task<CurrentUserTeamSummaryResponse> GetCurrentUserTeamSummaryAsync(string auth0UserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeamInviteSummaryResponse>> GetCurrentUserInvitesAsync(string auth0UserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeamInviteSummaryResponse>> GetCurrentUserSentInvitesAsync(string auth0UserId, CancellationToken cancellationToken = default);
    Task<TeamManagementSummaryResponse> LeaveTeamAsync(string auth0UserId, Guid teamId, CancellationToken cancellationToken = default);
    Task<TeamManagementSummaryResponse> RemoveMemberAsync(string auth0UserId, Guid teamId, Guid userId, CancellationToken cancellationToken = default);
    Task DeleteTeamAsync(string auth0UserId, Guid teamId, CancellationToken cancellationToken = default);
    Task<TeamInviteResponse> InviteUserAsync(string auth0UserId, Guid teamId, Guid userId, CancellationToken cancellationToken = default);
    Task<TeamInviteResponse> CancelInviteAsync(string auth0UserId, Guid teamId, Guid inviteId, CancellationToken cancellationToken = default);
    Task<TeamInviteResponse> RespondToInviteAsync(string auth0UserId, Guid inviteId, bool accept, CancellationToken cancellationToken = default);
    Task<TeamManagementSummaryResponse> TransferCaptainAsync(string auth0UserId, Guid teamId, Guid newCaptainUserId, CancellationToken cancellationToken = default);
    Task<TeamLogoResponse> UploadTeamLogoAsync(string auth0UserId, Guid teamId, IFormFile logo, CancellationToken cancellationToken = default);
    Task<TeamLogoResponse> RemoveTeamLogoAsync(string auth0UserId, Guid teamId, CancellationToken cancellationToken = default);
    Task<PublicTeamProfileResponse> GetPublicTeamProfileAsync(string teamName, CancellationToken cancellationToken = default);
}
