using Mercurius.Modules.Competition.Application.DTOs.Games;
using Mercurius.Modules.Competition.Application.DTOs.Placements;

namespace Mercurius.Modules.Competition.Application.Services;

internal interface IGameService
{
    Task CancelGameAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<GetPlacementDTO>> CompleteGameAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GetGameDTO> CreateGameAsync(CreateGameDTO createGameDTO, CancellationToken cancellationToken = default);
    Task DeleteGameAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<GetGameDTO>> GetAllGamesAsync(CancellationToken cancellationToken = default);
    Task<GetGameDTO> GetGameByIdAsync(Guid gameId, CancellationToken cancellationToken = default);
    Task<GetGameDTO> ReplaceSponsorPlacementsAsync(Guid id, ReplaceGameSponsorsDTO sponsorDTO, CancellationToken cancellationToken = default);
    Task ResetGameAsync(Guid id, CancellationToken cancellationToken = default);
    Task StartGameAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GetGameDTO> UpdateGameAsync(Guid id, UpdateGameDTO gameDTO, CancellationToken cancellationToken = default);
}
