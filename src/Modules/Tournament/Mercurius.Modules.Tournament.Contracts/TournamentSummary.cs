using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Tournament.Contracts;

public sealed record TournamentSummary(
    TournamentId Id,
    string Name,
    TournamentStatus Status,
    ParticipationMode ParticipationMode,
    int? TeamSize,
    DateTime PlannedStartTime,
    string? ImageUrl);
