using System.ComponentModel.DataAnnotations;

namespace Mercurius.Modules.Competition.Application.DTOs.Games;

public class RegisterGameTeamDTO
{
    [Required]
    public Guid TeamId { get; set; }
}
