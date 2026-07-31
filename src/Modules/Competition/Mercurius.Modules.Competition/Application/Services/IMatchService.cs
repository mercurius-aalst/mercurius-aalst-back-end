using Mercurius.Modules.Competition.Application.DTOs.Matches;

namespace Mercurius.Modules.Competition.Application.Services;

internal interface IMatchService
{
    Task<GetMatchDTO> GetMatchByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GetMatchDTO> UpdateMatchAsync(Guid id, UpdateMatchDTO updateMatchDTO, CancellationToken cancellationToken = default);
}
