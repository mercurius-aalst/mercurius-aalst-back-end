using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Competition.Contracts;

public sealed record MatchCompletedIntegrationEvent(MatchId MatchId, GameId GameId, Guid WinnerId);
