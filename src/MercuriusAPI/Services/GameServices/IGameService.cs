using Mercurius.LAN.API.DTOs.GameDTOs;
using Mercurius.LAN.API.DTOs.PlacementDTOs;
using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.Services.GameServices;

public interface IGameService
{
    Task<GetGameDTO> AddParticipantAsync(int id, Participant participant);
    Task CancelGameAsync(int id);
    Task<IEnumerable<GetPlacementDTO>> CompleteGameAsync(int id);
    Task<GetGameDTO> CreateGameAsync(CreateGameDTO createGameDTO);
    Task DeleteGameAsync(int id);
    IEnumerable<GetGameDTO> GetAllGames(string? academicSeason);
    Task<Game> GetGameByIdAsync(int gameId);
    Task<GetGameDTO> RemoveParticipantAsync(int id, Participant participant);
    Task ResetGameAsync(int id);
    Task StartGameAsync(int id);
    Task<GetGameDTO> UpdateGameAsync(int id, UpdateGameDTO gameDTO);
}
