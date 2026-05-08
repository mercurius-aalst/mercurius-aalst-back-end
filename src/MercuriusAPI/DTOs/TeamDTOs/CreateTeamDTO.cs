using System.ComponentModel.DataAnnotations;

namespace Mercurius.LAN.API.DTOs.TeamDTOs;

public class CreateTeamDTO
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; }
    public Guid CaptainUserId { get; set; }
}

