using Mercurius.Modules.Competition.Application.DTOs.Games;

namespace Mercurius.Modules.Competition.Application.Services;

internal interface IGameQueries
{
    Task<GetGameDTO> GetGameByIdAsync(Guid gameId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GetGameDTO>> GetAllGamesAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}
