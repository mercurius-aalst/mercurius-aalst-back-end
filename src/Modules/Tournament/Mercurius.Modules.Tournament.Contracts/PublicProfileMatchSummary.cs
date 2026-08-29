using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Tournament.Contracts;

/// <summary>
/// Public-safe match projection used by player and team profiles.
/// </summary>
public sealed record PublicProfileMatchSummary(
    MatchId MatchId,
    TournamentId TournamentId,
    string TournamentName,
    string? OpponentDisplayName,
    bool OpponentIsTbd,
    DateTime? EstimatedStartTime,
    DateTime? EstimatedEndTime,
    DateTime? ScheduledStartTime,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    MatchLifecycleState LifecycleState,
    MatchResultKind? ResultKind,
    int? ParticipantScore,
    int? OpponentScore,
    int RoundNumber,
    int MatchNumber,
    bool IsLowerBracketMatch);
