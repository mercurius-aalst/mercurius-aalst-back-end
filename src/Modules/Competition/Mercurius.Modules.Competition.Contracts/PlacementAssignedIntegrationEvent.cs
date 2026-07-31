using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Competition.Contracts;

public sealed record PlacementAssignedIntegrationEvent(GameId GameId, int Place, Guid ParticipantId);
