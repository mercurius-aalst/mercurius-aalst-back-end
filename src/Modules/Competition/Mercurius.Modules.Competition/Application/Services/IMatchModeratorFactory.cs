using Mercurius.Modules.Competition.Domain;

namespace Mercurius.Modules.Competition.Application.Services;

internal interface IMatchModeratorFactory
{
    IMatchModerator GetMatchModerator(BracketType bracketType);
}
