using System.ComponentModel.DataAnnotations;

namespace Mercurius.LAN.API.DTOs.TeamDTOs;

public class CreateTeamDTO
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; }
    [Range(1, int.MaxValue)]
    public int CaptainId { get; set; }
}

