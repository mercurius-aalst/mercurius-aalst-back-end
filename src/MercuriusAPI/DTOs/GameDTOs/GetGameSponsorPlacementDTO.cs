using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.DTOs.GameDTOs;

public class GetGameSponsorPlacementDTO
{
    public int Id { get; set; }
    public int SponsorId { get; set; }
    public string SponsorName { get; set; } = null!;
    public SponsorTier SponsorTier { get; set; }
    public string SponsorLogoUrl { get; set; } = null!;
    public string SponsorInfoUrl { get; set; } = null!;
    public string? SponsorDescription { get; set; }
    public SponsorContext Context { get; set; }
    public string? Headline { get; set; }
    public string? SupportLine { get; set; }
    public int DisplayOrder { get; set; }

    public GetGameSponsorPlacementDTO(GameSponsorPlacement placement)
    {
        Id = placement.Id;
        SponsorId = placement.SponsorId;
        SponsorName = placement.Sponsor.Name;
        SponsorTier = placement.Sponsor.SponsorTier;
        SponsorLogoUrl = placement.Sponsor.LogoUrl;
        SponsorInfoUrl = placement.Sponsor.InfoUrl;
        SponsorDescription = placement.Sponsor.Description;
        Context = placement.Context;
        Headline = placement.Headline;
        SupportLine = placement.SupportLine;
        DisplayOrder = placement.DisplayOrder;
    }
}
