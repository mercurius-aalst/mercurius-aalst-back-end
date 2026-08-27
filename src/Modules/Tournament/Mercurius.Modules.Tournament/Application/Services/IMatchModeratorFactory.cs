using Mercurius.Modules.Tournament.Domain;

namespace Mercurius.Modules.Tournament.Application.Services;

internal interface IMatchModeratorFactory
{
    IMatchModerator GetMatchModerator(BracketType bracketType);
}
