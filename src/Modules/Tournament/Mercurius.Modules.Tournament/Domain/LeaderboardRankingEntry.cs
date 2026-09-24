namespace Mercurius.Modules.Tournament.Domain;

internal sealed record LeaderboardRankingEntry(
    LeaderboardParticipant Participant,
    decimal? Score,
    long? DurationMilliseconds,
    int Rank);
