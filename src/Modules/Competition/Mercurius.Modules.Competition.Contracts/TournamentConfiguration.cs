using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Competition.Contracts;

public sealed record TournamentConfiguration(
    GameId GameId,
    BracketType BracketType,
    GameFormat Format,
    GameFormat FinalsFormat,
    ParticipationMode ParticipationMode,
    int? TeamSize,
    DateTime PlannedStartTime,
    int AverageGameDurationMinutes,
    int RoundBreakDurationMinutes);
