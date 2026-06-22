using Mercurius.LAN.API.DTOs.MatchDTOs;

namespace Mercurius.LAN.API.Services.MatchServices;

public interface IMatchService
{
    Task<GetMatchDTO> GetMatchByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GetMatchDTO> UpdateMatchAsync(Guid id, UpdateMatchDTO updateMatchDTO);
}
