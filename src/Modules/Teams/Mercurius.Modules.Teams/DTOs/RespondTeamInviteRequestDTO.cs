using System.ComponentModel.DataAnnotations;

namespace Mercurius.Modules.Teams.DTOs;

public sealed class RespondTeamInviteRequestDTO
{
    [Required]
    public bool Accept { get; set; }
}
