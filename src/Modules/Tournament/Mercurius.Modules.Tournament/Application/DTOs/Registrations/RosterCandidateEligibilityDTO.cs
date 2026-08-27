using Mercurius.Modules.Tournament.Application.DTOs.Participants;

namespace Mercurius.Modules.Tournament.Application.DTOs.Registrations;

internal record RosterCandidateEligibilityDTO(Guid UserId, PublicUserDTO? User, bool Eligible, IReadOnlyList<string> ReasonCodes);
