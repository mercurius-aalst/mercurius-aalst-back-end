namespace Mercurius.Modules.Competition.Application.DTOs.Registrations;

public record RosterCandidateEligibilityResponseDTO(bool Eligible, IReadOnlyList<string> ReasonCodes, IReadOnlyList<RosterCandidateEligibilityDTO> Candidates);
