using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Competition.Contracts;

public sealed record GameSummary(
    GameId Id,
    string Name,
    GameStatus Status,
    ParticipationMode ParticipationMode,
    int? TeamSize,
    DateTime PlannedStartTime,
    string? ImageUrl);
