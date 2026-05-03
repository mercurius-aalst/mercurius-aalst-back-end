using System.ComponentModel.DataAnnotations;

namespace Mercurius.LAN.API.DTOs.GameDTOs;

public class RegisterGameUserDTO
{
    [Range(1, int.MaxValue)]
    public Guid UserId { get; set; }
}
