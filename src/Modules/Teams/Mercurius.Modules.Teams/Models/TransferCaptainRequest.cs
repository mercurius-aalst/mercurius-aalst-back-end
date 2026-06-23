using System.ComponentModel.DataAnnotations;

namespace Mercurius.Modules.Teams.Models;

public sealed class TransferCaptainRequest
{
    [Required]
    public Guid NewCaptainUserId { get; set; }
}
