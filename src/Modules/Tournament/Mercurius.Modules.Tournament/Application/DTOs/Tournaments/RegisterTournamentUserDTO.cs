using System.ComponentModel.DataAnnotations;

namespace Mercurius.Modules.Tournament.Application.DTOs.Tournaments;

internal class RegisterTournamentUserDTO
{
    [Required]
    public Guid UserId { get; set; }
}
