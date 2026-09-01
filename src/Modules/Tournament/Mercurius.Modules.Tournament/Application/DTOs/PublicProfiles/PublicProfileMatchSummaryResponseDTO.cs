using Mercurius.Modules.Tournament.Contracts;

namespace Mercurius.Modules.Tournament.Application.DTOs.PublicProfiles;

internal sealed class PublicProfileMatchSummaryResponseDTO
{
    public Guid MatchId { get; init; }
    public Guid TournamentId { get; init; }
    public string TournamentName { get; init; } = string.Empty;
    public string? OpponentDisplayName { get; init; }
    public bool OpponentIsTbd { get; init; }
    public DateTime? EstimatedStartTime { get; init; }
    public DateTime? EstimatedEndTime { get; init; }
    public DateTime? ScheduledStartTime { get; init; }
    public DateTime? StartedAtUtc { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
    public MatchLifecycleState LifecycleState { get; init; }
    public MatchResultKind? ResultKind { get; init; }
    public int? ParticipantScore { get; init; }
    public int? OpponentScore { get; init; }
    public int RoundNumber { get; init; }
    public int MatchNumber { get; init; }
    public bool IsLowerBracketMatch { get; init; }
}
