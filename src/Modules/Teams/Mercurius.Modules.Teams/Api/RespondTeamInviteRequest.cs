using System.ComponentModel.DataAnnotations;

namespace Mercurius.Modules.Teams.Api;

public sealed class RespondTeamInviteRequest
{
    [Required]
    public bool Accept { get; set; }
}
