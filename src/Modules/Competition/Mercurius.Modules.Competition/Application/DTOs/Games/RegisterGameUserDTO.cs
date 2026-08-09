using System.ComponentModel.DataAnnotations;

namespace Mercurius.Modules.Competition.Application.DTOs.Games;

internal class RegisterGameUserDTO
{
    [Required]
    public Guid UserId { get; set; }
}
