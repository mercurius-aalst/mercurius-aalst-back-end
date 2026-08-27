using Mercurius.Modules.Sponsorship.Contracts;

namespace Mercurius.Modules.Sponsorship.Domain;

internal sealed class TournamentSponsorPlacement
{
    public int Id { get; set; }
    public Guid TournamentId { get; set; }
    public int SponsorId { get; set; }
    public SponsorContext Context { get; set; }
    public string? Headline { get; set; }
    public string? SupportLine { get; set; }
    public int DisplayOrder { get; set; }

    public Sponsor Sponsor { get; set; } = null!;
}
