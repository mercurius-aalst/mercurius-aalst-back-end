using Mercurius.Modules.Sponsorship.Contracts;
using System.ComponentModel.DataAnnotations;

namespace Mercurius.Modules.Tournament.Application.DTOs.Tournaments;

internal class TournamentSponsorPlacementInputDTO
{
    [Range(1, int.MaxValue)]
    public int SponsorId { get; set; }

    public SponsorContext Context { get; set; }

    [StringLength(160)]
    public string? Headline { get; set; }

    [StringLength(220)]
    public string? SupportLine { get; set; }

    public int DisplayOrder { get; set; }
}
