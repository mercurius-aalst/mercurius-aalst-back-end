namespace Mercurius.Modules.Tournament.Domain;

internal sealed class LeaderboardParticipant
{
    public Guid Id { get; set; }
    public Guid TournamentId { get; set; }
    public TournamentAggregate Tournament { get; set; } = null!;
    public Guid? LinkedUserId { get; set; }
    public string DisplayName { get; set; } = null!;
    public IList<LeaderboardAttempt> Attempts { get; set; } = [];
}

internal sealed class LeaderboardAttempt
{
    public Guid Id { get; set; }
    public Guid ParticipantId { get; set; }
    public LeaderboardParticipant Participant { get; set; } = null!;
    public decimal? Score { get; set; }
    public long? DurationMilliseconds { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public Guid RowVersion { get; set; }
}

internal sealed class PlacementLeaderboardParticipant
{
    public Guid PlacementId { get; set; }
    public Placement Placement { get; set; } = null!;
    public Guid LeaderboardParticipantId { get; set; }
    public LeaderboardParticipant LeaderboardParticipant { get; set; } = null!;
}
