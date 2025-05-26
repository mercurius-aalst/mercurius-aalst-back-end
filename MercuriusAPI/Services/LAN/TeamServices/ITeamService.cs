using MercuriusAPI.DTOs.LAN.TeamDTOs;
using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.Services.LAN.TeamServices
{
    public interface ITeamService
    {
        Task<GetTeamDTO> AddPlayerAsync(int id, Player player);
        Task<GetTeamDTO> CreateTeamAsync(CreateTeamDTO teamDTO, Player player);
        Task DeleteTeamAsync(int teamId);
        IEnumerable<GetTeamDTO> GetAllTeams();
        Task<Team> GetTeamByIdAsync(int teamId);
        Task<GetTeamDTO> RemovePlayerAsync(int id, int playerId);
    }
}