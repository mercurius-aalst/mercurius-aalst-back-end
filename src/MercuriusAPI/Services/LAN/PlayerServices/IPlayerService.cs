using MercuriusAPI.DTOs.LAN.PlayerDTOs;
using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.Services.LAN.PlayerServices
{
    public interface IPlayerService
    {
        Task<GetPlayerDTO> CreatePlayerAsync(CreatePlayerDTO playerDTO);
        Task DeletePlayerAsync(int playerId);
        IEnumerable<GetPlayerDTO> GetAllPlayers();
        Task<Player> GetPlayerByIdAsync(int playerId);
        Task<GetPlayerDTO> UpdatePlayerAsync(int id, UpdatePlayerDTO player);
    }
}