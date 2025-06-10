using System.ComponentModel.DataAnnotations;

namespace MercuriusAPI.DTOs.LAN.TeamDTOs
{
    public class UpdateTeamDTO
    {
        [Required]
        public string Name { get; set; }
        public IFormFile? Picture { get; set; }
        [Required]
        public int CaptainId { get; set; }
    }
}
