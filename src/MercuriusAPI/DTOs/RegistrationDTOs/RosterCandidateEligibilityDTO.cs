using Mercurius.Modules.Identity.DTOs;

namespace Mercurius.LAN.API.DTOs.RegistrationDTOs;

public record RosterCandidateEligibilityDTO(Guid UserId, PublicUserDTO? User, bool Eligible, IReadOnlyList<string> ReasonCodes);
