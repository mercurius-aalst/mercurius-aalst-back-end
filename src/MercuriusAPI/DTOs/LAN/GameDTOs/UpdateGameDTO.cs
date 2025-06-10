using MercuriusAPI.Models.LAN;
using System.ComponentModel.DataAnnotations;

namespace MercuriusAPI.DTOs.LAN.GameDTOs
{
    public class UpdateGameDTO
    {
        [Required]
        public string Name { get; set; }
        public IFormFile? Picture { get; set; }
        [Required]
        public GameFormat Format { get; set; }
        [Required]
        public GameFormat FinalsFormat { get; set; }
        [Required]
        public BracketType BracketType { get; set; }
        
    }
}
