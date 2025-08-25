using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.DTOs.LAN.GameDTOs
{
    public class UpdateGameDTO
    {
        public string Name { get; set; }
        public GameFormat Format { get; set; }
        public GameFormat FinalsFormat { get; set; }
        public BracketType BracketType { get; set; }
        public IFormFile? Image { get; set; }
        public string RegisterFormUrl { get; set; }
    }
}
