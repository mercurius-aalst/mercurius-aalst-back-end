using System.ComponentModel.DataAnnotations;

namespace Mercurius.Modules.Teams.DTOs;

public sealed class TransferCaptainRequestDTO
{
    [Required]
    public Guid NewCaptainUserId { get; set; }
}
