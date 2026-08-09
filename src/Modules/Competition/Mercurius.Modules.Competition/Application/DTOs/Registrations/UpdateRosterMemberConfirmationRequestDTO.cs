using Mercurius.Modules.Competition.Contracts;

namespace Mercurius.Modules.Competition.Application.DTOs.Registrations;

internal sealed class UpdateRosterMemberConfirmationRequestDTO
{
    public RosterMemberConfirmationStatus? ConfirmationStatus { get; set; }
}
