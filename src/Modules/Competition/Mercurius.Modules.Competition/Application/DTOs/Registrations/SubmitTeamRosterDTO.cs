namespace Mercurius.Modules.Competition.Application.DTOs.Registrations;

internal record SubmitTeamRosterDTO(Guid TeamId, IReadOnlyList<Guid> UserIds);
