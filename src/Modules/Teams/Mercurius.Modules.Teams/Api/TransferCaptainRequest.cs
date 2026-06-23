using System.ComponentModel.DataAnnotations;

namespace Mercurius.Modules.Teams.Api;

public sealed class TransferCaptainRequest
{
    [Required]
    public Guid NewCaptainUserId { get; set; }
}
