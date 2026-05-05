using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.DTOs.MatchDTOs;
using Mercurius.LAN.API.Models;
using Mercurius.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.LAN.API.Services.MatchServices;

public class MatchService : IMatchService
{
    private readonly MercuriusDBContext _dbContext;

    public MatchService(MercuriusDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetMatchDTO> UpdateMatchAsync(Guid id, UpdateMatchDTO updateMatchDTO)
    {
        var match = await GetMatchByIdForUpdateAsync(id);
        match.SetScoresAndWinner(updateMatchDTO.Participant1Score, updateMatchDTO.Participant2Score);
        _dbContext.Matches.Update(match);
        await _dbContext.SaveChangesAsync();
        return new GetMatchDTO(match);
    }

    public async Task<Match> GetMatchByIdForUpdateAsync(Guid id)
    {
        var match = await _dbContext.Matches
            .Include(m => m.WinnerNextMatch)
                .ThenInclude(m => m.UserParticipant1)
            .Include(m => m.WinnerNextMatch)
                .ThenInclude(m => m.UserParticipant2)
            .Include(m => m.WinnerNextMatch)
                .ThenInclude(m => m.TeamParticipant1)
            .Include(m => m.WinnerNextMatch)
                .ThenInclude(m => m.TeamParticipant2)
            .Include(m => m.WinnerNextMatch)
                .ThenInclude(m => m.WinnerNextMatch)
            .Include(m => m.WinnerNextMatch)
                .ThenInclude(m => m.LoserNextMatch)
            .Include(m => m.LoserNextMatch)
                .ThenInclude(m => m.UserParticipant1)
            .Include(m => m.LoserNextMatch)
                .ThenInclude(m => m.UserParticipant2)
            .Include(m => m.LoserNextMatch)
                .ThenInclude(m => m.TeamParticipant1)
            .Include(m => m.LoserNextMatch)
                .ThenInclude(m => m.TeamParticipant2)
            .Include(m => m.LoserNextMatch)
                .ThenInclude(m => m.WinnerNextMatch)
            .Include(m => m.LoserNextMatch)
                .ThenInclude(m => m.LoserNextMatch)
            .Include(m => m.UserParticipant1)
            .Include(m => m.UserParticipant2)
            .Include(m => m.TeamParticipant1)
            .Include(m => m.TeamParticipant2)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (match is null)
            throw new NotFoundException($"{nameof(Match)} not found");

        return match;
    }

    public async Task<Match> GetMatchByIdAsync(Guid id)
    {
        var match = await _dbContext.Matches
            .Include(m => m.UserParticipant1)
            .Include(m => m.UserParticipant2)
            .Include(m => m.TeamParticipant1)
            .Include(m => m.TeamParticipant2)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (match is null)
            throw new NotFoundException($"{nameof(Match)} not found");

        return match;
    }
}

