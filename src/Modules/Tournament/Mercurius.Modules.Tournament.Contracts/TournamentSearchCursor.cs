namespace Mercurius.Modules.Tournament.Contracts;

public sealed record TournamentSearchCursor(
    int RelevanceRank,
    string NormalizedLabel,
    int TypeOrder,
    Guid StableId);
