namespace Mercurius.Modules.Tournament.Contracts;

public sealed record PublicProfileMatchSummarySet(
    IReadOnlyList<PublicProfileMatchSummary> PreviousMatches,
    IReadOnlyList<PublicProfileMatchSummary> UpcomingMatches);
