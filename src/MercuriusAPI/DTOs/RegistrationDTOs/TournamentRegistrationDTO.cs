using Mercurius.LAN.API.DTOs.Auth;
using Mercurius.LAN.API.DTOs.TeamDTOs;
using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.DTOs.RegistrationDTOs;

public class TournamentRegistrationDTO
{
    public Guid Id { get; set; }
    public Guid GameId { get; set; }
    public TournamentRegistrationKind Kind { get; set; }
    public TournamentRegistrationStatus Status { get; set; }
    public PublicUserDTO? User { get; set; }
    public GetTeamDTO? Team { get; set; }
    public IReadOnlyList<TournamentRosterMemberDTO> RosterMembers { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public TournamentRegistrationDTO()
    {
    }

    public TournamentRegistrationDTO(TournamentRegistration registration)
    {
        Id = registration.Id;
        GameId = registration.GameId;
        Kind = registration.Kind;
        Status = registration.Status;
        User = registration.User is null ? null : new PublicUserDTO(registration.User);
        Team = registration.Team is null ? null : new GetTeamDTO(registration.Team);
        CreatedAtUtc = registration.CreatedAtUtc;
        UpdatedAtUtc = registration.UpdatedAtUtc;
        RosterMembers = registration.RosterMembers
            .OrderBy(member => member.IsCaptain ? 0 : 1)
            .ThenBy(member => member.User.Username)
            .Select(member => new TournamentRosterMemberDTO
            {
                Id = member.Id,
                User = new PublicUserDTO(member.User),
                IsCaptain = member.IsCaptain,
                ConfirmationStatus = member.ConfirmationStatus
            })
            .ToList();
    }
}
