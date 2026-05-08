using System.ComponentModel.DataAnnotations;

namespace Mercurius.LAN.API.DTOs.GameDTOs;

public class RegisterGameUserDTO
{
    [Required]
    public Guid UserId { get; set; }
}
