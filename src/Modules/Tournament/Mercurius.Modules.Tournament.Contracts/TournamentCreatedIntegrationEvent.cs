using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Tournament.Contracts;

public sealed record TournamentCreatedIntegrationEvent(TournamentId TournamentId, string Name);
