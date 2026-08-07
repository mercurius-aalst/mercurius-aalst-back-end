using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Competition.Contracts;

public sealed record GameCanceledIntegrationEvent(GameId GameId, string Name);
