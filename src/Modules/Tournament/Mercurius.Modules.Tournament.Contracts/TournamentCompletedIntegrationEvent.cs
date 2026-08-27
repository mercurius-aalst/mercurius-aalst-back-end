using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Tournament.Contracts;

public sealed record TournamentCompletedIntegrationEvent(TournamentId TournamentId, DateTime CompletedAtUtc);
