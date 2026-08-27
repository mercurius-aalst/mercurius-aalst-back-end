using Mercurius.Modules.Tournament.Application.DTOs.Matches;
using Mercurius.Modules.Tournament.Domain;
using Mercurius.Modules.Tournament.Infrastructure;
using Mercurius.Modules.Shared;
using Mercurius.Modules.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Platform.Eventing;
using MatchCompletedIntegrationEvent =
    Mercurius.Modules.Tournament.Contracts.MatchCompletedIntegrationEvent;

namespace Mercurius.Modules.Tournament.Application.Services;

internal sealed class MatchService : IMatchService
{
    private readonly ITournamentDbContext _dbContext;
    private readonly IModuleEventPublisher _moduleEventPublisher;

    public MatchService(
        ITournamentDbContext dbContext,
        IModuleEventPublisher moduleEventPublisher)
    {
        _dbContext = dbContext;
        _moduleEventPublisher = moduleEventPublisher;
    }

    public async Task<GetMatchDTO> UpdateMatchAsync(
        Guid id,
        UpdateMatchDTO updateMatchDTO,
        CancellationToken cancellationToken = default)
    {
        var match = await _dbContext.Matches
            .Include(candidate => candidate.WinnerNextMatch)
                .ThenInclude(next => next!.WinnerNextMatch)
            .Include(candidate => candidate.WinnerNextMatch)
                .ThenInclude(next => next!.LoserNextMatch)
            .Include(candidate => candidate.LoserNextMatch)
                .ThenInclude(next => next!.WinnerNextMatch)
            .Include(candidate => candidate.LoserNextMatch)
                .ThenInclude(next => next!.LoserNextMatch)
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (match is null)
            throw new NotFoundException($"{nameof(Match)} not found");

        var previousWinnerId = match.GetWinnerId();
        match.SetScoresAndWinner(updateMatchDTO.Participant1Score, updateMatchDTO.Participant2Score);
        if (!previousWinnerId.HasValue && match.GetWinnerId().HasValue)
        {
            _moduleEventPublisher.Publish(new MatchCompletedIntegrationEvent(
                new MatchId(match.Id),
                new TournamentId(match.TournamentId),
                match.GetWinnerId()!.Value));
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
        return TournamentDtoMapper.ToGetMatchDto(match);
    }

    public async Task<GetMatchDTO> GetMatchByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var match = await _dbContext.Matches
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (match is null)
            throw new NotFoundException($"{nameof(Match)} not found");

        return TournamentDtoMapper.ToGetMatchDto(match);
    }
}
