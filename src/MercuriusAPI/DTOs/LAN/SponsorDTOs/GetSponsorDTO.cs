using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.DTOs.LAN.SponsorDTOs;

public class GetSponsorDTO
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int SponsorTier { get; set; }
    public string LogoUrl { get; set; }
    public string InfoUrl { get; set; }

    public GetSponsorDTO(Sponsor sponsor)
    {
        Id = sponsor.Id;
        Name = sponsor.Name;
        SponsorTier = sponsor.SponsorTier;
        LogoUrl = sponsor.LogoUrl;
        InfoUrl = sponsor.InfoUrl;
    }
}
