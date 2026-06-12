namespace Mercurius.LAN.API.DTOs.RegistrationDTOs;

public record SubmitTeamRosterDTO(Guid TeamId, IReadOnlyList<Guid> UserIds);
