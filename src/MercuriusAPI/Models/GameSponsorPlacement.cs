namespace Mercurius.LAN.API.Models;

public class GameSponsorPlacement
{
    public int Id { get; set; }
    public Guid GameId { get; set; }
    public int SponsorId { get; set; }
    public SponsorContext Context { get; set; }
    public string? Headline { get; set; }
    public string? SupportLine { get; set; }
    public int DisplayOrder { get; set; }

    public Game Game { get; set; } = null!;
    public Sponsor Sponsor { get; set; } = null!;
}
