namespace Mercurius.Modules.Tournament.Application.DTOs.Registrations;

internal record SubmitTeamRosterDTO(Guid TeamId, IReadOnlyList<Guid> UserIds);
