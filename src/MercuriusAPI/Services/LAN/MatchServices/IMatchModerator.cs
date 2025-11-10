using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.Services.LAN.MatchServices;

public interface IMatchModerator
{
    IEnumerable<Match> GenerateMatchesForGame(Game game);
    void DeterminePlacements(Game game);
}
