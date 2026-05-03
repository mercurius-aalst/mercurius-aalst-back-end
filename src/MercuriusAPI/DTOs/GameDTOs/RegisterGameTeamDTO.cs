using System.ComponentModel.DataAnnotations;

namespace Mercurius.LAN.API.DTOs.GameDTOs;

public class RegisterGameTeamDTO
{
    [Required]
    public Guid TeamId { get; set; }
}
