using Mercurius.LAN.API.DTOs.Auth;
using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.DTOs.RegistrationDTOs;

public class TournamentRosterMemberDTO
{
    public Guid Id { get; set; }
    public PublicUserDTO User { get; set; }
    public bool IsCaptain { get; set; }
    public RosterMemberConfirmationStatus ConfirmationStatus { get; set; }
}
