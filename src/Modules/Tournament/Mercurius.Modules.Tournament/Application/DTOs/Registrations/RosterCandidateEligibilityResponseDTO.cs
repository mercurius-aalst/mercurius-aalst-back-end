namespace Mercurius.Modules.Tournament.Application.DTOs.Registrations;

internal record RosterCandidateEligibilityResponseDTO(bool Eligible, IReadOnlyList<string> ReasonCodes, IReadOnlyList<RosterCandidateEligibilityDTO> Candidates);
