using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.DTOs.SponsorDTOs;

public class UpdateSponsorDTO
{
    public string Name { get; set; } = null!;
    public SponsorTier SponsorTier { get; set; }
    public IFormFile? Logo { get; set; }
    public string InfoUrl { get; set; } = null!;
    public string? Description { get; set; }
}

