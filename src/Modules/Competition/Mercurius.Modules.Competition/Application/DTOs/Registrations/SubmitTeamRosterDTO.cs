namespace Mercurius.Modules.Competition.Application.DTOs.Registrations;

public record SubmitTeamRosterDTO(Guid TeamId, IReadOnlyList<Guid> UserIds);
