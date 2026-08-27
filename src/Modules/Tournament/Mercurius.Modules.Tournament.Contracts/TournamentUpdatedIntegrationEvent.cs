using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Tournament.Contracts;

public sealed record TournamentUpdatedIntegrationEvent(TournamentId TournamentId, string Name);
