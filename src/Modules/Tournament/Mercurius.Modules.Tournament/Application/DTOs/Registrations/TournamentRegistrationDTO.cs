using Mercurius.Modules.Tournament.Application.DTOs.Participants;
using Mercurius.Modules.Tournament.Contracts;

namespace Mercurius.Modules.Tournament.Application.DTOs.Registrations;

internal class TournamentRegistrationDTO
{
    public Guid Id { get; set; }
    public Guid TournamentId { get; set; }
    public TournamentRegistrationKind Kind { get; set; }
    public TournamentRegistrationStatus Status { get; set; }
    public PublicUserDTO? User { get; set; }
    public TeamParticipantDTO? Team { get; set; }
    public IReadOnlyList<TournamentRosterMemberDTO> RosterMembers { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public TournamentRegistrationDTO()
    {
    }

}
