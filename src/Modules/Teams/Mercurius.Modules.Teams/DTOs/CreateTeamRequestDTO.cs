using System.ComponentModel.DataAnnotations;

namespace Mercurius.Modules.Teams.DTOs;

public sealed class CreateTeamRequestDTO
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    public Guid CaptainUserId { get; set; }
}
