using Mercurius.Modules.Competition.Domain;

namespace Mercurius.Modules.Competition.Application.Services;

internal interface IMatchModerator
{
    IEnumerable<Match> GenerateMatchesForGame(Game game);
    void DeterminePlacements(Game game);
}

