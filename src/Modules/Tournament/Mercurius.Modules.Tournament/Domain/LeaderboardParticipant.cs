using Mercurius.Modules.Shared.Exceptions;

namespace Mercurius.Modules.Tournament.Domain;

internal sealed class LeaderboardParticipant
{
    public Guid Id { get; set; }
    public Guid TournamentId { get; set; }
    public TournamentAggregate Tournament { get; set; } = null!;
    public Guid? LinkedUserId { get; set; }
    public string DisplayName { get; set; } = null!;
    public IList<LeaderboardAttempt> Attempts { get; set; } = [];

    public decimal? BestScore => Attempts
        .Where(attempt => attempt.Score.HasValue)
        .Select(attempt => attempt.Score)
        .Max();

    public long? BestDurationMilliseconds => Attempts
        .Where(attempt => attempt.DurationMilliseconds.HasValue)
        .Select(attempt => attempt.DurationMilliseconds)
        .Min();

    public LeaderboardAttempt AddAttempt(decimal? score, long? durationMilliseconds, DateTime nowUtc)
    {
        var attempt = new LeaderboardAttempt
        {
            Id = Guid.NewGuid(),
            ParticipantId = Id,
            Score = score,
            DurationMilliseconds = durationMilliseconds,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
            RowVersion = Guid.NewGuid()
        };
        Attempts.Add(attempt);
        return attempt;
    }
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

    public void EnsureRowVersion(Guid rowVersion)
    {
        if (RowVersion != rowVersion)
            throw new ConflictException("leaderboard_attempt_changed", "The leaderboard attempt changed. Refresh and try again.");
    }

    public void Correct(decimal? score, long? durationMilliseconds, DateTime nowUtc)
    {
        Score = score;
        DurationMilliseconds = durationMilliseconds;
        UpdatedAtUtc = nowUtc;
        RowVersion = Guid.NewGuid();
    }
}

internal sealed class PlacementLeaderboardParticipant
{
    public Guid PlacementId { get; set; }
    public Placement Placement { get; set; } = null!;
    public Guid LeaderboardParticipantId { get; set; }
    public LeaderboardParticipant LeaderboardParticipant { get; set; } = null!;
}
