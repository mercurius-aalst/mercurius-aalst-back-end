namespace Mercurius.Modules.Competition.Application.DTOs.Registrations;

public record EligibilityResponseDTO(bool Eligible, IReadOnlyList<string> ReasonCodes);
