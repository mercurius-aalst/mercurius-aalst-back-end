using MercuriusAPI.Data;
using MercuriusAPI.DTOs.LAN.MatchDTOs;
using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.Services.LAN.MatchServices
{
    public class MatchService
    {
        private readonly MercuriusDBContext _dbContext;
        private readonly IMatchGeneratorFactory _matchGeneratorFactory;

        public MatchService(MercuriusDBContext dbContext, IMatchGeneratorFactory matchGeneratorFactory)
        {
            _dbContext = dbContext;
            _matchGeneratorFactory = matchGeneratorFactory;
        }
        public async Task<GetMatchDTO> UpdateMatchAsync(int id, UpdateMatchDTO updateMatchDTO)
        {
            var match = await GetMatchByIdAsync(id);
            match.SetScoresAndWinner(updateMatchDTO.Participant1Score, updateMatchDTO.Participant2Score);
           

            var matchGenerator = _matchGeneratorFactory.GetMatchGenerator(match.Game.BracketType);
            matchGenerator.AssignNextMatches(match, match.Game.Matches);
            //Should also assign participants to next match based on the winner and on the BracketType


            _dbContext.Matches.Update(match);
            await _dbContext.SaveChangesAsync();
            return new GetMatchDTO(match);
        }

        public async Task<Match> GetMatchByIdAsync(int id)
        {
            var match = await _dbContext.Matches.FindAsync(id);
            if(match is null)
                throw new Exception("Match not found");
            return match;
        }
    }
}
