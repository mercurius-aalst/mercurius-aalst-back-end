using Mercurius.LAN.API.DTOs.Auth;
using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.DTOs.RegistrationDTOs;

public class PublicTournamentRegistrationDTO
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

    public PublicTournamentRegistrationDTO(TournamentRegistration registration)
    {
        Id = registration.Id;
        GameId = registration.GameId;
        Kind = registration.Kind;
        Status = registration.Status;
        User = registration.User is null ? null : new PublicUserDTO(registration.User);
        Team = registration.Team is null ? null : new PublicTournamentTeamDTO(registration.Team);
        RosterMembers = registration.RosterMembers
            .OrderBy(member => member.IsCaptain ? 0 : 1)
            .ThenBy(member => member.User.Username)
            .Select(member => new PublicTournamentRosterMemberDTO
            {
                User = new PublicUserDTO(member.User),
                IsCaptain = member.IsCaptain
            })
            .ToList();
    }
}
