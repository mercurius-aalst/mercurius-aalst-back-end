using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.Services.MatchServices;

public interface IMatchModeratorFactory
{
    IMatchModerator GetMatchModerator(BracketType bracketType);
}
