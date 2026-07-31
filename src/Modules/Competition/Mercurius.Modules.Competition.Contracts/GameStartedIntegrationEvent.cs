using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Competition.Contracts;

public sealed record GameStartedIntegrationEvent(GameId GameId, DateTime StartedAtUtc);
