using System.ComponentModel.DataAnnotations;

namespace Mercurius.LAN.API.DTOs.GameDTOs;

public class ReplaceGameSponsorsDTO
{
    [Required]
    public List<GameSponsorPlacementInputDTO> SponsorPlacements { get; set; } = [];
}
