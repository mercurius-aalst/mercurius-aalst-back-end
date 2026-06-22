using Mercurius.LAN.API.DTOs.GameDTOs;
using Mercurius.LAN.API.DTOs.PlacementDTOs;

namespace Mercurius.LAN.API.Services.GameServices;

public interface IGameService
{
    Task CancelGameAsync(Guid id);
    Task<IEnumerable<GetPlacementDTO>> CompleteGameAsync(Guid id);
    Task<GetGameDTO> CreateGameAsync(CreateGameDTO createGameDTO);
    Task DeleteGameAsync(Guid id);
    Task<IEnumerable<GetGameDTO>> GetAllGamesAsync(CancellationToken cancellationToken = default);
    Task<GetGameDTO> GetGameByIdAsync(Guid gameId, CancellationToken cancellationToken = default);
    Task<GetGameDTO> ReplaceSponsorPlacementsAsync(Guid id, ReplaceGameSponsorsDTO sponsorDTO);
    Task ResetGameAsync(Guid id);
    Task StartGameAsync(Guid id);
    Task<GetGameDTO> UpdateGameAsync(Guid id, UpdateGameDTO gameDTO);
}
