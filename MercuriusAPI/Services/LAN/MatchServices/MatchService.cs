using MercuriusAPI.Data;
using MercuriusAPI.DTOs.LAN.MatchDTOs;
using MercuriusAPI.Models.LAN;

namespace MercuriusAPI.Services.LAN.MatchServices
{
    public class MatchService : IMatchService
    {
        private readonly MercuriusDBContext _dbContext;
        private readonly IMatchModeratorFactory _matchModeratorFactory;

        public MatchService(MercuriusDBContext dbContext, IMatchModeratorFactory matchModeratorFactory)
        {
            _dbContext = dbContext;
            _matchModeratorFactory = matchModeratorFactory;
        }
        public async Task<GetMatchDTO> UpdateMatchAsync(int id, UpdateMatchDTO updateMatchDTO)
        {
            var match = await GetMatchByIdAsync(id);
            match.SetScoresAndWinner(updateMatchDTO.Participant1Score, updateMatchDTO.Participant2Score);         
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
