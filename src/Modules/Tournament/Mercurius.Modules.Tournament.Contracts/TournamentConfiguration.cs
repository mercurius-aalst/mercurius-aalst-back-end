using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Tournament.Contracts;

public sealed record TournamentConfiguration(
    TournamentId TournamentId,
    BracketType BracketType,
    GameFormat Format,
    GameFormat FinalsFormat,
    ParticipationMode ParticipationMode,
    int? TeamSize,
    DateTime PlannedStartTime,
    int AverageGameDurationMinutes,
    int RoundBreakDurationMinutes);
