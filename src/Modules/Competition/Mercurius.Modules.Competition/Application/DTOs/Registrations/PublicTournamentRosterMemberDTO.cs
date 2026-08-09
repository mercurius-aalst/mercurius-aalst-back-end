using Mercurius.Modules.Competition.Application.DTOs.Participants;

namespace Mercurius.Modules.Competition.Application.DTOs.Registrations;

internal class PublicTournamentRosterMemberDTO
{
    public PublicUserDTO User { get; set; } = null!;
    public bool IsCaptain { get; set; }
}
