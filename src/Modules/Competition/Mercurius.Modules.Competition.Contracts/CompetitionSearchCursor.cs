namespace Mercurius.Modules.Competition.Contracts;

public sealed record CompetitionSearchCursor(
    int RelevanceRank,
    string NormalizedLabel,
    int TypeOrder,
    Guid StableId);
