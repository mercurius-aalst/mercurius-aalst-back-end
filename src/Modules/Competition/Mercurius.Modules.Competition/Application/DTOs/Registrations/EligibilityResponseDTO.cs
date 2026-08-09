namespace Mercurius.Modules.Competition.Application.DTOs.Registrations;

internal record EligibilityResponseDTO(bool Eligible, IReadOnlyList<string> ReasonCodes);
