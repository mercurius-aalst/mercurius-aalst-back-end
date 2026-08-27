namespace Mercurius.Modules.Tournament.Application.DTOs.Registrations;

internal record EligibilityResponseDTO(bool Eligible, IReadOnlyList<string> ReasonCodes);
