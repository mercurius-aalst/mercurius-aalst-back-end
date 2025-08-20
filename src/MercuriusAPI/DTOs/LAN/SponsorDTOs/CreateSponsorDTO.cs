using System.ComponentModel.DataAnnotations;

namespace MercuriusAPI.DTOs.LAN.SponsorDTOs
{
    public class CreateSponsorDTO
    {
        public string Name { get; set; }
        public int SponsorTier { get; set; }
        public IFormFile Logo { get; set; }
        public string InfoUrl { get; set; }
    }
}
