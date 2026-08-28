using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Tournament.Contracts;

public sealed record PlacementAssignedIntegrationEvent(TournamentId TournamentId, int Place, Guid ParticipantId);
