using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Competition.Contracts;

public sealed record GameCreatedIntegrationEvent(GameId GameId, string Name);
