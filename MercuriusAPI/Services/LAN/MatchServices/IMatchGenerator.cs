using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.Services.LAN.MatchServices
{
    public interface IMatchGenerator
    {
        IEnumerable<Match> GenerateMatchesForGame(Game game);
    }
}
