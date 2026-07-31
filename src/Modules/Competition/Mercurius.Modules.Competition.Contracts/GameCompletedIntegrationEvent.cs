using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Competition.Contracts;

public sealed record GameCompletedIntegrationEvent(GameId GameId, DateTime CompletedAtUtc);
