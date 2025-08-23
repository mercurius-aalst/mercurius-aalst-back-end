using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.DTOs.LAN.GameDTOs
{
    public class CreateGameDTO
    {
        public string Name { get; set; }
        public BracketType BracketType { get; set; }
        public GameFormat Format { get; set; }
        public GameFormat FinalsFormat { get; set; }
        public ParticipantType ParticipantType { get; set; }
        public IFormFile Image { get; set; }
    }
}
