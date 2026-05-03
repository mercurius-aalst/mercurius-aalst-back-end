using Mercurius.LAN.API.DTOs.TeamDTOs;
using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.Services.TeamServices;

public interface ITeamService
{
    Task<GetTeamDTO> CreateTeamAsync(CreateTeamDTO teamDTO);
    Task DeleteTeamAsync(int teamId);
    IEnumerable<GetTeamDTO> GetAllTeams();
    Task<IEnumerable<TeamInviteDTO>> GetUserInvitesAsync(int userId);
    Task<Team> GetTeamByIdAsync(int teamId);
    Task<TeamInviteDTO> InviteUserAsync(int teamId, int userId);
    Task<GetTeamDTO> RemoveMemberAsync(int id, int userId);
    Task<TeamInviteDTO> RespondToInviteAsync(int teamId, int userId, bool accept);
    Task<GetTeamDTO> UpdateTeamAsync(int id, UpdateTeamDTO teamDTO);
}
