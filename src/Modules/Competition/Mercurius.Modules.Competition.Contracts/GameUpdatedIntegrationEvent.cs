using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Competition.Contracts;

public sealed record GameUpdatedIntegrationEvent(GameId GameId, string Name);
