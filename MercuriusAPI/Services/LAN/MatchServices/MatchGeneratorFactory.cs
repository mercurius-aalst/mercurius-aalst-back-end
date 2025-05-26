using MercuriusAPI.Models.LAN;
using MercuriusAPI.Services.LAN.MatchServices.BracketTypes;

namespace MercuriusAPI.Services.LAN.MatchServices
{
    public class MatchGeneratorFactory : IMatchGeneratorFactory
    {
        private readonly IServiceProvider _serviceProvider;
        public MatchGeneratorFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public IMatchGenerator GetMatchGenerator(BracketType bracketType)
        {
            return bracketType switch
            {
                BracketType.SingleElimination => _serviceProvider.GetRequiredService<SingleEliminationMatchGenerator>(),
                BracketType.DoubleElimination => _serviceProvider.GetRequiredService<DoubleEliminationMatchGenerator>(),
                BracketType.RoundRobin => _serviceProvider.GetRequiredService<RoundRobinMatchGenerator>(),
                _ => throw new NotSupportedException($"Bracket type {bracketType} is not (yet) supported.")
            };
        }
    }
}
