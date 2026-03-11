using Mercurius.LAN.API.DTOs.PlayerDTOs;
using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.Services.PlayerServices;

public interface IPlayerService
{
    Task<GetPlayerDTO> CreatePlayerAsync(CreatePlayerDTO playerDTO);
    Task DeletePlayerAsync(int playerId);
    IEnumerable<GetPlayerDTO> GetAllPlayers();
    Task<Player> GetPlayerByIdAsync(int playerId);
    Task<GetPlayerDTO> UpdatePlayerAsync(int id, UpdatePlayerDTO player);
}
