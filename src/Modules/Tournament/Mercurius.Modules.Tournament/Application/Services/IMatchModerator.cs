using Mercurius.Modules.Tournament.Domain;

namespace Mercurius.Modules.Tournament.Application.Services;

internal interface IMatchModerator
{
    IEnumerable<Match> GenerateMatchesForTournament(TournamentAggregate tournament);
    void DeterminePlacements(TournamentAggregate tournament);
}

