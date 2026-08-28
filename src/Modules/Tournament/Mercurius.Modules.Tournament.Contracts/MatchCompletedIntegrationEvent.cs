using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Tournament.Contracts;

public sealed record MatchCompletedIntegrationEvent(MatchId MatchId, TournamentId TournamentId, Guid WinnerId);
