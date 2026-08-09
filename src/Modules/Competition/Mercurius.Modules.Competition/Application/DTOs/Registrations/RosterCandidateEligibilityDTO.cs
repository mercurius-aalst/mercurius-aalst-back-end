using Mercurius.Modules.Competition.Application.DTOs.Participants;

namespace Mercurius.Modules.Competition.Application.DTOs.Registrations;

internal record RosterCandidateEligibilityDTO(Guid UserId, PublicUserDTO? User, bool Eligible, IReadOnlyList<string> ReasonCodes);
