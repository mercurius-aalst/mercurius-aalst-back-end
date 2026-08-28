using System.ComponentModel.DataAnnotations;

namespace Mercurius.Modules.Tournament.Application.DTOs.Tournaments;

internal class ReplaceTournamentSponsorsDTO
{
    [Required]
    public List<TournamentSponsorPlacementInputDTO> SponsorPlacements { get; set; } = [];
}
