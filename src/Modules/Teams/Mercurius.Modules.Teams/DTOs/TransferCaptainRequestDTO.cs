using System.ComponentModel.DataAnnotations;

namespace Mercurius.Modules.Teams.DTOs;

internal sealed class TransferCaptainRequestDTO
{
    [Required]
    public Guid NewCaptainUserId { get; set; }
}
