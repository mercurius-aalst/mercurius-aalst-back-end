namespace Mercurius.LAN.API.DTOs.RegistrationDTOs;

public record EligibilityResponseDTO(bool Eligible, IReadOnlyList<string> ReasonCodes);
