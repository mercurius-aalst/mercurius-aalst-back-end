using Mercurius.Modules.Shared.Exceptions;

namespace Mercurius.Modules.Tournament.Domain;

internal enum LeaderboardRankingMetric
{
    HighestScore = 0,
    FastestTime = 1
}

internal static class LeaderboardRankingMetricRules
{
    public static void ValidateAttemptValue(this LeaderboardRankingMetric metric, decimal? score, long? durationMilliseconds)
    {
        switch (metric)
        {
            case LeaderboardRankingMetric.HighestScore:
                if (!score.HasValue || durationMilliseconds.HasValue || score.Value < 0 || score.Value >= 1_000_000_000_000m || GetScale(score.Value) > 6)
                    throw new ValidationException("Highest-score attempts require a non-negative score with at most 12 integral and 6 fractional digits.");
                return;
            case LeaderboardRankingMetric.FastestTime:
                if (!durationMilliseconds.HasValue || score.HasValue || durationMilliseconds.Value <= 0)
                    throw new ValidationException("Fastest-time attempts require a positive durationMilliseconds value.");
                return;
            default:
                throw new ValidationException("Tournament has no supported leaderboard ranking metric.");
        }
    }

    private static int GetScale(decimal value) => (decimal.GetBits(value)[3] >> 16) & 0x7F;
}
