using MercuriusAPI.DTOs.LAN.TeamDTOs;
using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.Services.LAN.TeamServices
{
    public interface ITeamService
    {
        Task<GetTeamDTO> CreateTeamAsync(CreateTeamDTO teamDTO, Player player);
        Task DeleteTeamAsync(int teamId);
        IEnumerable<GetTeamDTO> GetAllTeams();
        Task<IEnumerable<TeamInviteDTO>> GetPlayerInvitesAsync(int playerId);
        Task<Team> GetTeamByIdAsync(int teamId);
        Task<TeamInviteDTO> InvitePlayerAsync(int teamId, int playerId);
        Task<GetTeamDTO> RemovePlayerAsync(int id, int playerId);
        Task<TeamInviteDTO> RespondToInviteAsync(int teamId, int playerId, bool accept);
        Task<GetTeamDTO> UpdateTeamAsync(int id, UpdateTeamDTO teamDTO);
    }
}