using System.ComponentModel.DataAnnotations;

namespace Mercurius.Modules.Competition.Application.DTOs.Games;

internal class ReplaceGameSponsorsDTO
{
    [Required]
    public List<GameSponsorPlacementInputDTO> SponsorPlacements { get; set; } = [];
}
