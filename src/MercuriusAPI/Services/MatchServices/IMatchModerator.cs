using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.Services.MatchServices;

public interface IMatchModerator
{
    IEnumerable<Match> GenerateMatchesForGame(Game game);
    void DeterminePlacements(Game game);
}

