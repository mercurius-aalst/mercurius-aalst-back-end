using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Tournament.Contracts;

public sealed record MatchResultReversedIntegrationEvent(
    MatchId MatchId,
    TournamentId TournamentId);
