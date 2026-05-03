using Mercurius.LAN.API.DTOs.MatchDTOs;
using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.Services.MatchServices;

public interface IMatchService
{
    Task<Match> GetMatchByIdAsync(Guid id);
    Task<GetMatchDTO> UpdateMatchAsync(Guid id, UpdateMatchDTO updateMatchDTO);
}
