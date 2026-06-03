using Mercurius.LAN.API.DTOs.TeamDTOs;
using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.Services.TeamServices;

public interface ITeamService
{
    Task<GetTeamDTO> CreateTeamAsync(CreateTeamDTO teamDTO);
    Task DeleteTeamAsync(Guid teamId);
    IEnumerable<GetTeamDTO> GetAllTeams();
    Task<PublicTeamProfileDTO> GetPublicTeamProfileAsync(string teamName);
    Task<IEnumerable<TeamInviteDTO>> GetUserInvitesAsync(Guid userId);
    Task<Team> GetTeamByIdAsync(Guid teamId);
    Task<Team> GetTeamByNameAsync(string name);
    Task<TeamInviteDTO> InviteUserAsync(Guid teamId, Guid userId);
    Task<GetTeamDTO> RemoveMemberAsync(Guid id, Guid userId);
    Task<TeamInviteDTO> RespondToInviteAsync(Guid teamId, Guid userId, bool accept);
    Task<IEnumerable<GetTeamDTO>> SearchTeamsByNameAsync(string query, int? limit = null);
    Task<GetTeamDTO> UpdateTeamAsync(Guid id, UpdateTeamDTO teamDTO);
}
