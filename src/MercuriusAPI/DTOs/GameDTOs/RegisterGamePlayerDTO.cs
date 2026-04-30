using System.ComponentModel.DataAnnotations;

namespace Mercurius.LAN.API.DTOs.GameDTOs;

public class RegisterGamePlayerDTO
{
    [Range(1, int.MaxValue)]
    public int PlayerId { get; set; }
}
