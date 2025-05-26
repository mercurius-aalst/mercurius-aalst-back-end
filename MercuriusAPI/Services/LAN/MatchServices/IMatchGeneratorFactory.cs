using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.Services.LAN.MatchServices
{
    public interface IMatchGeneratorFactory
    {
        IMatchGenerator GetMatchGenerator(BracketType bracketType);
    }
}