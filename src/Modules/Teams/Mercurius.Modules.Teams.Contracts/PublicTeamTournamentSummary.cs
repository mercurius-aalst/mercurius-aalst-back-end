using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Teams.Contracts;

public sealed record PublicTeamTournamentSummary(
    GameId GameId,
    string Name);
