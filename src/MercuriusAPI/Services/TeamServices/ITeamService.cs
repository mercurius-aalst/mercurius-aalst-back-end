using Mercurius.LAN.API.DTOs.TeamDTOs;
using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.Services.TeamServices;

public interface ITeamService
{
    Task<GetTeamDTO> CreateTeamAsync(CreateTeamDTO teamDTO);
    Task<TeamManagementSummaryDTO> CreateCurrentUserTeamAsync(string auth0UserId, CreateTeamDTO teamDTO);
    Task DeleteTeamAsync(Guid teamId);
    IEnumerable<GetTeamDTO> GetAllTeams();
    Task<CurrentUserTeamSummaryDTO> GetCurrentUserTeamSummaryAsync(string auth0UserId);
    Task<PublicTeamProfileDTO> GetPublicTeamProfileAsync(string teamName);
    Task<IEnumerable<TeamInviteDTO>> GetUserInvitesAsync(Guid userId);
    Task<IEnumerable<TeamInviteSummaryDTO>> GetCurrentUserInvitesAsync(string auth0UserId);
    Task<IEnumerable<TeamInviteSummaryDTO>> GetCurrentUserSentInvitesAsync(string auth0UserId);
    Task<Team> GetTeamByIdAsync(Guid teamId);
    Task<Team> GetTeamByNameAsync(string name);
    Task<TeamInviteDTO> InviteUserAsync(Guid teamId, Guid userId);
    Task<TeamInviteDTO> InviteUserAsync(string auth0UserId, Guid teamId, Guid userId);
    Task<TeamInviteDTO> CancelInviteAsync(string auth0UserId, Guid teamId, Guid inviteId);
    Task<GetTeamDTO> RemoveMemberAsync(Guid id, Guid userId);
    Task<TeamManagementSummaryDTO> LeaveTeamAsync(string auth0UserId, Guid teamId);
    Task<TeamInviteDTO> RespondToInviteAsync(Guid teamId, Guid userId, bool accept);
    Task<TeamInviteDTO> RespondToInviteAsync(string auth0UserId, Guid inviteId, bool accept);
    Task<TeamManagementSummaryDTO> TransferCaptainAsync(string auth0UserId, Guid teamId, Guid newCaptainUserId);
    Task<TeamLogoResponseDTO> UploadTeamLogoAsync(string auth0UserId, Guid teamId, IFormFile logo);
    Task<TeamLogoResponseDTO> RemoveTeamLogoAsync(string auth0UserId, Guid teamId);
    Task<IEnumerable<GetTeamDTO>> SearchTeamsByNameAsync(string query, int? limit = null);
    Task<GetTeamDTO> UpdateTeamAsync(Guid id, UpdateTeamDTO teamDTO);
}
