using System.ComponentModel.DataAnnotations;
Add commentMore actions

namespace MercuriusAPI.DTOs.LAN.TeamDTOs
{
    public class TeamInviteDTO
    {
        public int Id { get; set; }
        public int TeamId { get; set; }
        public int PlayerId { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
    }

    public class CreateTeamInviteDTO
    {
        [Required]
        public int TeamId { get; set; }
        [Required]
        public int PlayerId { get; set; }
    }

    public class RespondTeamInviteDTO
    {
        [Required]
        public bool Accept { get; set; }
    }
}