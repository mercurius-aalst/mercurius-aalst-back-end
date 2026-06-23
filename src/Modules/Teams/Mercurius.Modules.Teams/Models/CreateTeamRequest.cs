using System.ComponentModel.DataAnnotations;

namespace Mercurius.Modules.Teams.Models;

public sealed class CreateTeamRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    public Guid CaptainUserId { get; set; }
}
