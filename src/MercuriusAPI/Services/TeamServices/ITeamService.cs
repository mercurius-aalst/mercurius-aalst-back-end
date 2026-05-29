using Mercurius.LAN.API.DTOs.TeamDTOs;
using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.Services.TeamServices;

public interface ITeamService
{
    Task<GetTeamDTO> CreateTeamAsync(CreateTeamDTO teamDTO);
    Task DeleteTeamAsync(Guid teamId);
    IEnumerable<GetTeamDTO> GetAllTeams();
    IEnumerable<GetPublicTeamDTO> GetAllPublicTeams(bool includePlatformIds);
    Task<IEnumerable<TeamInviteDTO>> GetUserInvitesAsync(Guid userId);
    Task<Team> GetTeamByIdAsync(Guid teamId);
    Task<GetPublicTeamDTO> GetPublicTeamByIdAsync(Guid teamId, bool includePlatformIds);
    Task<TeamInviteDTO> InviteUserAsync(Guid teamId, Guid userId);
    Task<GetTeamDTO> RemoveMemberAsync(Guid id, Guid userId);
    Task<TeamInviteDTO> RespondToInviteAsync(Guid teamId, Guid userId, bool accept);
    Task<GetTeamDTO> UpdateTeamAsync(Guid id, UpdateTeamDTO teamDTO);
}
