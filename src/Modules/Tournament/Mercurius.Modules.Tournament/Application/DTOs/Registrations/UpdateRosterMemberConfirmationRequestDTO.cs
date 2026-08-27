using Mercurius.Modules.Tournament.Contracts;

namespace Mercurius.Modules.Tournament.Application.DTOs.Registrations;

internal sealed class UpdateRosterMemberConfirmationRequestDTO
{
    public RosterMemberConfirmationStatus? ConfirmationStatus { get; set; }
}
