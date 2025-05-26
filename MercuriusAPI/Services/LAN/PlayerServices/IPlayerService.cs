using MercuriusAPI.DTOs.LAN.PlayerDTOs;
using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.Services.LAN.PlayerServices
{
    public interface IPlayerService
    {
        Task<Player> CreatePlayerAsync(CreatePlayerDTO playerDTO);
        Task DeletePlayerAsync(int playerId);
        IEnumerable<Player> GetAllPlayers();
        Task<Player> GetPlayerByIdAsync(int playerId);
        Task<Player> UpdatePlayerAsync(int id, UpdatePlayerDTO player);
    }
}