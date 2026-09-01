using System.ComponentModel.DataAnnotations;

namespace Mercurius.Modules.Tournament.Application.DTOs.Matches;

internal sealed class SubmitMatchScoreDTO
{
    [Range(0, int.MaxValue)]
    public int Participant1Score { get; set; }

    [Range(0, int.MaxValue)]
    public int Participant2Score { get; set; }
}
