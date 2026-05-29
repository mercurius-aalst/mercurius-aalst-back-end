using Mercurius.LAN.API.DTOs.GameDTOs;
using Mercurius.LAN.API.DTOs.PlacementDTOs;
using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.Services.GameServices;

public interface IGameService
{
    Task CancelGameAsync(Guid id);
    Task<IEnumerable<GetPlacementDTO>> CompleteGameAsync(Guid id);
    Task<GetGameDTO> CreateGameAsync(CreateGameDTO createGameDTO);
    Task DeleteGameAsync(Guid id);
    IEnumerable<GetGameDTO> GetAllGames();
    IEnumerable<GetPublicGameDTO> GetAllPublicGames(bool includePlatformIds);
    Task<Game> GetGameByIdAsync(Guid gameId);
    Task<GetPublicGameDTO> GetPublicGameByIdAsync(Guid gameId, bool includePlatformIds);
    Task<GetGameDTO> RegisterUserAsync(Guid id, Guid userId);
    Task<GetGameDTO> RegisterTeamAsync(Guid id, Guid teamId);
    Task<GetGameDTO> ReplaceSponsorPlacementsAsync(Guid id, ReplaceGameSponsorsDTO sponsorDTO);
    Task ResetGameAsync(Guid id);
    Task StartGameAsync(Guid id);
    Task<GetGameDTO> UnregisterUserAsync(Guid id, Guid userId);
    Task<GetGameDTO> UnregisterTeamAsync(Guid id, Guid teamId);
    Task<GetGameDTO> UpdateGameAsync(Guid id, UpdateGameDTO gameDTO);
}
