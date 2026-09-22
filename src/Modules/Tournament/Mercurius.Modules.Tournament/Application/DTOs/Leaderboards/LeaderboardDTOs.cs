using Mercurius.Modules.Tournament.Contracts;
using System.ComponentModel.DataAnnotations;

namespace Mercurius.Modules.Tournament.Application.DTOs.Leaderboards;

internal sealed class LeaderboardResponseDTO
{
    public Guid TournamentId { get; set; }
    public LeaderboardRankingMetric RankingMetric { get; set; }
    public IReadOnlyList<LeaderboardRowDTO> Rows { get; set; } = [];
}

internal sealed class LeaderboardRowDTO
{
    public int Rank { get; set; }
    public Guid ParticipantId { get; set; }
    public string DisplayName { get; set; } = null!;
    public LeaderboardParticipantKind ParticipantKind { get; set; }
    public Guid? LinkedUserId { get; set; }
    public decimal? Score { get; set; }
    public long? DurationMilliseconds { get; set; }
}

internal sealed class AdminLeaderboardResponseDTO
{
    public Guid TournamentId { get; set; }
    public LeaderboardRankingMetric RankingMetric { get; set; }
    public IReadOnlyList<AdminLeaderboardParticipantDTO> Participants { get; set; } = [];
}

internal sealed class AdminLeaderboardParticipantDTO
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = null!;
    public LeaderboardParticipantKind ParticipantKind { get; set; }
    public Guid? LinkedUserId { get; set; }
    public IReadOnlyList<LeaderboardAttemptDTO> Attempts { get; set; } = [];
}

internal sealed class LeaderboardAttemptDTO
{
    public Guid Id { get; set; }
    public decimal? Score { get; set; }
    public long? DurationMilliseconds { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public Guid RowVersion { get; set; }
}

internal sealed class RecordLeaderboardAttemptDTO
{
    public Guid? ParticipantId { get; set; }
    public Guid? LinkedUserId { get; set; }
    [StringLength(100, MinimumLength = 1)]
    public string? GuestDisplayName { get; set; }
    public decimal? Score { get; set; }
    public long? DurationMilliseconds { get; set; }
}

internal sealed class UpdateLeaderboardAttemptDTO
{
    public decimal? Score { get; set; }
    public long? DurationMilliseconds { get; set; }
    [Required]
    public Guid? RowVersion { get; set; }
}
