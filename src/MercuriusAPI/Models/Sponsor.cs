namespace Mercurius.LAN.API.Models;

public class Sponsor
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public SponsorTier SponsorTier { get; set; }
    public string LogoUrl { get; set; } = null!;
    public string InfoUrl { get; set; } = null!;
    public string? Description { get; set; }

    public IList<GameSponsorPlacement> GameSponsorPlacements { get; set; } = [];
}

