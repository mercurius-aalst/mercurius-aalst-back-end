namespace Mercurius.LAN.API.DTOs.RegistrationDTOs;

public record RosterCandidateEligibilityResponseDTO(bool Eligible, IReadOnlyList<string> ReasonCodes, IReadOnlyList<RosterCandidateEligibilityDTO> Candidates);
