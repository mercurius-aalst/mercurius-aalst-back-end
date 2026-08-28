using Mercurius.Modules.Tournament.Application.DTOs.Participants;

namespace Mercurius.Modules.Tournament.Application.DTOs.Registrations;

internal class PublicTournamentRosterMemberDTO
{
    public PublicUserDTO User { get; set; } = null!;
    public bool IsCaptain { get; set; }
}
