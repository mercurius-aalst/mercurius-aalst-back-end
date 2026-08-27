using System.ComponentModel.DataAnnotations;

namespace Mercurius.Modules.Tournament.Application.DTOs.Tournaments;

internal class RegisterTournamentTeamDTO
{
    [Required]
    public Guid TeamId { get; set; }
}
