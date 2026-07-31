using System.ComponentModel.DataAnnotations;

namespace Mercurius.Modules.Competition.Application.DTOs.Games;

public class RegisterGameUserDTO
{
    [Required]
    public Guid UserId { get; set; }
}
