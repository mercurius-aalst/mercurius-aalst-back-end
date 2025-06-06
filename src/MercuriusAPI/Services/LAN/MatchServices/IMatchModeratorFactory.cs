using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.Services.LAN.MatchServices
{
    public interface IMatchModeratorFactory
    {
        IMatchModerator GetMatchModerator(BracketType bracketType);
    }
}