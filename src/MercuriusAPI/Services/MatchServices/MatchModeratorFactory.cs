using Mercurius.LAN.API.Models;
using Mercurius.LAN.API.Services.MatchServices.BracketTypes;

namespace Mercurius.LAN.API.Services.MatchServices;

public class MatchModeratorFactory : IMatchModeratorFactory
{
    private readonly IServiceProvider _serviceProvider;
    public MatchModeratorFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    public IMatchModerator GetMatchModerator(BracketType bracketType)
    {
        return bracketType switch
        {
            BracketType.SingleElimination => _serviceProvider.GetRequiredService<SingleEliminationMatchModerator>(),
            BracketType.DoubleElimination => _serviceProvider.GetRequiredService<DoubleEliminationMatchModerator>(),
            BracketType.RoundRobin => _serviceProvider.GetRequiredService<RoundRobinMatchModerator>(),
            _ => throw new NotSupportedException($"Bracket type {bracketType} is not (yet) supported.")
        };
    }
}

