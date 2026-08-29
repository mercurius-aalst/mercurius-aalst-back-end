using Mercurius.Modules.Tournament.Contracts;

namespace Mercurius.Modules.Tournament.Application.DTOs.Matches;

internal sealed class GetMatchActionStateDTO
{
    public GetMatchDTO Match { get; set; } = null!;
    public MatchParticipantSide? AuthorizedParticipant { get; set; }
    public bool CanConfirmEnded { get; set; }
    public bool CanSubmitScore { get; set; }
    public bool CanForfeit { get; set; }
    public int? Participant1ReportedScore1 { get; set; }
    public int? Participant1ReportedScore2 { get; set; }
    public int? Participant2ReportedScore1 { get; set; }
    public int? Participant2ReportedScore2 { get; set; }
}
