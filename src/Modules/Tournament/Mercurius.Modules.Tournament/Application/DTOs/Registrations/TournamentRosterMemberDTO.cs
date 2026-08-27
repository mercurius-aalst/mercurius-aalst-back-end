using Mercurius.Modules.Tournament.Application.DTOs.Participants;
using Mercurius.Modules.Tournament.Contracts;

namespace Mercurius.Modules.Tournament.Application.DTOs.Registrations;

internal class TournamentRosterMemberDTO
{
    public Guid Id { get; set; }
    public PublicUserDTO User { get; set; } = null!;
    public bool IsCaptain { get; set; }
    public RosterMemberConfirmationStatus ConfirmationStatus { get; set; }
}
