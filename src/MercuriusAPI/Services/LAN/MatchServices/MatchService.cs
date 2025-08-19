using MercuriusAPI.Data;
using MercuriusAPI.DTOs.LAN.MatchDTOs;
using MercuriusAPI.Exceptions;
using MercuriusAPI.Models.LAN;
using Microsoft.EntityFrameworkCore;

namespace MercuriusAPI.Services.LAN.MatchServices
{
    public class MatchService : IMatchService
    {
        private readonly MercuriusDBContext _dbContext;

        public MatchService(MercuriusDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<GetMatchDTO> UpdateMatchAsync(int id, UpdateMatchDTO updateMatchDTO)
        {
            var match = await GetMatchByIdForUpdateAsync(id);
            match.SetScoresAndWinner(updateMatchDTO.Participant1Score, updateMatchDTO.Participant2Score);         
            _dbContext.Matches.Update(match);
            await _dbContext.SaveChangesAsync();
            return new GetMatchDTO(match);
        }

        public async Task<Match> GetMatchByIdForUpdateAsync(int id)
        {
            var match = await _dbContext.Matches
                .Include(m => m.WinnerNextMatch)
                    .ThenInclude(m => m.Participant1)
                .Include(m => m.WinnerNextMatch)
                    .ThenInclude(m => m.Participant2)
                .Include(m => m.WinnerNextMatch)
                    .ThenInclude(m => m.WinnerNextMatch)
                .Include(m => m.WinnerNextMatch)
                    .ThenInclude(m => m.LoserNextMatch)
                .Include(m => m.LoserNextMatch)
                    .ThenInclude(m => m.Participant1)
                .Include(m => m.LoserNextMatch)
                    .ThenInclude(m => m.Participant2)
                .Include(m => m.LoserNextMatch)
                    .ThenInclude(m => m.WinnerNextMatch)
                .Include(m => m.LoserNextMatch)
                    .ThenInclude(m => m.LoserNextMatch)
                .Include(m => m.Participant1)
                .Include(m => m.Participant2)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match is null)
                throw new NotFoundException($"{nameof(Match)} not found");

            return match;
        }

        public async Task<Match> GetMatchByIdAsync(int id)
        {
            var match = await _dbContext.Matches
                .Include(m => m.Participant1)
                .Include(m => m.Participant2)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (match is null)
                throw new NotFoundException($"{nameof(Match)} not found");

            return match;
        }
    }
}
