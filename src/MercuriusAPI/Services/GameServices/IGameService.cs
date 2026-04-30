using Mercurius.LAN.API.DTOs.GameDTOs;
using Mercurius.LAN.API.DTOs.PlacementDTOs;
using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.Services.GameServices;

public interface IGameService
{
    Task CancelGameAsync(int id);
    Task<IEnumerable<GetPlacementDTO>> CompleteGameAsync(int id);
    Task<GetGameDTO> CreateGameAsync(CreateGameDTO createGameDTO);
    Task DeleteGameAsync(int id);
    IEnumerable<GetGameDTO> GetAllGames();
    Task<Game> GetGameByIdAsync(int gameId);
    Task<GetGameDTO> RegisterPlayerAsync(int id, int playerId);
    Task<GetGameDTO> RegisterTeamAsync(int id, int teamId);
    Task ResetGameAsync(int id);
    Task StartGameAsync(int id);
    Task<GetGameDTO> UnregisterPlayerAsync(int id, int playerId);
    Task<GetGameDTO> UnregisterTeamAsync(int id, int teamId);
    Task<GetGameDTO> UpdateGameAsync(int id, UpdateGameDTO gameDTO);
}
