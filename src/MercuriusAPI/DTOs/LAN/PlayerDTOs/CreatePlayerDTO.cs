using System.ComponentModel.DataAnnotations;

namespace MercuriusAPI.DTOs.LAN.PlayerDTOs
{
    public class CreatePlayerDTO
    {
        [Required]
        public string Username { get; set; }
        [Required]
        public string Firstname { get; set; }
        [Required]
        public string Lastname { get; set; }
        [Required]
        public string Email { get; set; }
        public IFormFile? Picture { get; set; }
        public string? DiscordId { get; set; }
        public string? SteamId { get; set; }
        public string? RiotId { get; set; }

    }
}
