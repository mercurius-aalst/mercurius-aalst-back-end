using Mercurius.LAN.API.DTOs.MatchDTOs;
using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.Services.MatchServices;

public interface IMatchService
{
    Task<Match> GetMatchByIdAsync(int id);
    Task<GetMatchDTO> UpdateMatchAsync(int id, UpdateMatchDTO updateMatchDTO);
}
