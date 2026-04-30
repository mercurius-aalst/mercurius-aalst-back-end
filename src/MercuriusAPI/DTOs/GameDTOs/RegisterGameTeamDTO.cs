using System.ComponentModel.DataAnnotations;

namespace Mercurius.LAN.API.DTOs.GameDTOs;

public class RegisterGameTeamDTO
{
    [Range(1, int.MaxValue)]
    public int TeamId { get; set; }
}
