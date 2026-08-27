using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Teams.Contracts;

public sealed record PublicTeamTournamentSummary(
    TournamentId TournamentId,
    string Name);
