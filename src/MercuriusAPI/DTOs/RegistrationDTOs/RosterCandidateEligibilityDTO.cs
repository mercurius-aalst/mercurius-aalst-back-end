using Mercurius.LAN.API.DTOs.Auth;

namespace Mercurius.LAN.API.DTOs.RegistrationDTOs;

public record RosterCandidateEligibilityDTO(Guid UserId, PublicUserDTO? User, bool Eligible, IReadOnlyList<string> ReasonCodes);
