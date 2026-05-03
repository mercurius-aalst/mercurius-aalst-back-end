using System.ComponentModel.DataAnnotations;

namespace Mercurius.LAN.API.DTOs.GameDTOs;

public class RegisterGameUserDTO
{
    [Range(1, int.MaxValue)]
    public int UserId { get; set; }
}
