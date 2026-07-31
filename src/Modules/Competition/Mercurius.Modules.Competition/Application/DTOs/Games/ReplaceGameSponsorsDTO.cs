using System.ComponentModel.DataAnnotations;

namespace Mercurius.Modules.Competition.Application.DTOs.Games;

public class ReplaceGameSponsorsDTO
{
    [Required]
    public List<GameSponsorPlacementInputDTO> SponsorPlacements { get; set; } = [];
}
