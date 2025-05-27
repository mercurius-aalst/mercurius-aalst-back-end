using MercuriusAPI.DTOs.LAN.MatchDTOs;
using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.Services.LAN.MatchServices
{
    public interface IMatchService
    {
        Task<Match> GetMatchByIdAsync(int id);
        Task<IEnumerable<GetMatchDTO>> UpdateMatchAsync(int id, UpdateMatchDTO updateMatchDTO);
    }
}