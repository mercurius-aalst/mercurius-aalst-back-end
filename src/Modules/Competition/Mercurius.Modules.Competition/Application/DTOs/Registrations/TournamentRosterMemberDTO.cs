using Mercurius.Modules.Competition.Application.DTOs.Participants;
using Mercurius.Modules.Competition.Contracts;

namespace Mercurius.Modules.Competition.Application.DTOs.Registrations;

internal class TournamentRosterMemberDTO
{
    public Guid Id { get; set; }
    public PublicUserDTO User { get; set; } = null!;
    public bool IsCaptain { get; set; }
    public RosterMemberConfirmationStatus ConfirmationStatus { get; set; }
}
