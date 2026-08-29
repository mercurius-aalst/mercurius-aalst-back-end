using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Tournament.Contracts;

public sealed record MatchResolutionRequiredIntegrationEvent(
    MatchId MatchId,
    TournamentId TournamentId,
    Guid? AssignedAdminUserId = null);
