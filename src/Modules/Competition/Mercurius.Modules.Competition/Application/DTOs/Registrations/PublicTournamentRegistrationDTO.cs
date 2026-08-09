using Mercurius.Modules.Competition.Application.DTOs.Participants;
using Mercurius.Modules.Competition.Contracts;

namespace Mercurius.Modules.Competition.Application.DTOs.Registrations;

internal class PublicTournamentRegistrationDTO
{
    public Guid Id { get; set; }
    public Guid GameId { get; set; }
    public TournamentRegistrationKind Kind { get; set; }
    public TournamentRegistrationStatus Status { get; set; }
    public PublicUserDTO? User { get; set; }
    public PublicTournamentTeamDTO? Team { get; set; }
    public IReadOnlyList<PublicTournamentRosterMemberDTO> RosterMembers { get; set; } = [];

    public PublicTournamentRegistrationDTO()
    {
    }

}
