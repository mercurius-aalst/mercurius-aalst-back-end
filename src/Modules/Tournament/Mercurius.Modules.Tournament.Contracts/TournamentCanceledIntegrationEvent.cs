using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Tournament.Contracts;

public sealed record TournamentCanceledIntegrationEvent(TournamentId TournamentId, string Name);
