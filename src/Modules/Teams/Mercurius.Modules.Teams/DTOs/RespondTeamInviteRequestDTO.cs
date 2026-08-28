using System.ComponentModel.DataAnnotations;

namespace Mercurius.Modules.Teams.DTOs;

internal sealed class RespondTeamInviteRequestDTO
{
    [Required]
    public bool Accept { get; set; }
}
