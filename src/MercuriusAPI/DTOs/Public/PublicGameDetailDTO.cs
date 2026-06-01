using Mercurius.LAN.API.DTOs.MatchDTOs;

namespace Mercurius.LAN.API.DTOs.Public;

public class PublicGameDetailDTO : PublicGameSummaryDTO
{
    public IEnumerable<PublicPlacementDTO> Placements { get; set; } = [];
    public IEnumerable<GetMatchDTO> Matches { get; set; } = [];
}
