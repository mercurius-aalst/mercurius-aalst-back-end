using Mercurius.Modules.Competition.Application.DTOs.Games;

namespace Mercurius.Modules.Competition.Application.Services;

internal interface IGameQueries
{
    Task<GetGameDTO> GetGameByIdAsync(Guid gameId, CancellationToken cancellationToken = default);

    Task<IEnumerable<GetGameDTO>> GetAllGamesAsync(CancellationToken cancellationToken = default);
}
