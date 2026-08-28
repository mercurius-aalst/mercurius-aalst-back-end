using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Tournament.Contracts;

public sealed record TournamentStartedIntegrationEvent(TournamentId TournamentId, DateTime StartedAtUtc);
