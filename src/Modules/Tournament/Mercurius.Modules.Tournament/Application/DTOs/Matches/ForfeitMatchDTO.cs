using Mercurius.Modules.Tournament.Contracts;

namespace Mercurius.Modules.Tournament.Application.DTOs.Matches;

internal sealed class ForfeitMatchDTO
{
    public MatchParticipantSide? Participant { get; set; }
}
