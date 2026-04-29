using System.ComponentModel.DataAnnotations;

namespace Mercurius.LAN.API.DTOs.MatchDTOs;

public class UpdateMatchDTO
{
    [Range(0, int.MaxValue)]
    public int Participant1Score { get; set; }
    [Range(0, int.MaxValue)]
    public int Participant2Score { get; set; }
}

