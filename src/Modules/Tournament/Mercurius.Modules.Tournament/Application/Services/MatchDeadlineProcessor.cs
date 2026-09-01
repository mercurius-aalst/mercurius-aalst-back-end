using Mercurius.Modules.Tournament.Domain;
using Mercurius.Modules.Tournament.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.Eventing;
using MatchCompletedIntegrationEvent = Mercurius.Modules.Tournament.Contracts.MatchCompletedIntegrationEvent;
using MatchResolutionRequiredIntegrationEvent = Mercurius.Modules.Tournament.Contracts.MatchResolutionRequiredIntegrationEvent;

namespace Mercurius.Modules.Tournament.Application.Services;

internal sealed class MatchDeadlineProcessor : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MatchDeadlineProcessor> _logger;

    public MatchDeadlineProcessor(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<MatchDeadlineProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredMatchesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unable to process expired tournament match result windows.");
            }

            try
            {
                await Task.Delay(PollInterval, _timeProvider, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ProcessExpiredMatchesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ITournamentDbContext>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IModuleEventPublisher>();
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var matches = await CreateExpiredDeadlineQuery(dbContext, nowUtc)
            .ToListAsync(cancellationToken);

        var changed = false;
        var changedTournaments = new List<TournamentAggregate>();
        foreach (var match in matches)
        {
            var tournament = match.Tournament;
            var beforeState = match.LifecycleState;
            var beforeResult = match.HasResult;
            match.ApplyDeadline(nowUtc);
            if (beforeState == match.LifecycleState && beforeResult == match.HasResult)
                continue;

            changed = true;
            if (!changedTournaments.Contains(tournament))
                changedTournaments.Add(tournament);
            if (!beforeResult && match.HasResult && match.GetWinnerId() is Guid winnerId)
            {
                await LoadDirectNextMatchesAsync(dbContext, match, cancellationToken);
                match.UpdateParticipantsNextMatch();
                eventPublisher.Publish(new MatchCompletedIntegrationEvent(
                    new Mercurius.Modules.Shared.MatchId(match.Id),
                    new Mercurius.Modules.Shared.TournamentId(match.TournamentId),
                    winnerId),
                    nowUtc);
            }
            else if (beforeState != MatchLifecycleState.AdminResolutionRequired &&
                    match.LifecycleState == MatchLifecycleState.AdminResolutionRequired)
            {
                eventPublisher.Publish(new MatchResolutionRequiredIntegrationEvent(
                    new Mercurius.Modules.Shared.MatchId(match.Id),
                    new Mercurius.Modules.Shared.TournamentId(match.TournamentId),
                    tournament.AssignedAdminUserId),
                    nowUtc);
            }
        }

        if (!changed)
            return;

        var changedTournamentIds = changedTournaments
            .Select(tournament => tournament.Id)
            .ToArray();
        var inProgressTournamentIds = await dbContext.Tournaments
            .AsNoTracking()
            .Where(tournament =>
                changedTournamentIds.Contains(tournament.Id) &&
                tournament.Status == TournamentStatus.InProgress)
            .Select(tournament => tournament.Id)
            .ToListAsync(cancellationToken);
        if (inProgressTournamentIds.Count != changedTournaments.Count)
            return;

        foreach (var tournament in changedTournaments)
            dbContext.Tournaments.Entry(tournament).Property(candidate => candidate.Status).IsModified = true;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static IQueryable<Match> CreateExpiredDeadlineQuery(
        ITournamentDbContext dbContext,
        DateTime nowUtc) =>
        dbContext.Matches
            .Include(match => match.Tournament)
            .Where(match =>
                match.Tournament.Status == TournamentStatus.InProgress &&
                ((match.LifecycleState == MatchLifecycleState.ScoreConfirmation &&
                  match.ScoreConfirmationDeadlineUtc <= nowUtc) ||
                 (match.LifecycleState == MatchLifecycleState.Disputed &&
                  match.CorrectionDeadlineUtc <= nowUtc)));

    private static async Task LoadDirectNextMatchesAsync(
        ITournamentDbContext dbContext,
        Match match,
        CancellationToken cancellationToken)
    {
        var nextMatchIds = new[] { match.WinnerNextMatchId, match.LoserNextMatchId }
            .Where(nextMatchId => nextMatchId.HasValue)
            .Select(nextMatchId => nextMatchId!.Value)
            .Distinct()
            .ToArray();
        if (nextMatchIds.Length == 0)
            return;

        var nextMatches = await dbContext.Matches
            .Where(candidate =>
                candidate.TournamentId == match.TournamentId &&
                nextMatchIds.Contains(candidate.Id))
            .ToListAsync(cancellationToken);
        var nextMatchesById = nextMatches.ToDictionary(candidate => candidate.Id);
        if (match.WinnerNextMatchId is { } winnerNextMatchId)
            match.WinnerNextMatch = nextMatchesById.GetValueOrDefault(winnerNextMatchId);
        if (match.LoserNextMatchId is { } loserNextMatchId)
            match.LoserNextMatch = nextMatchesById.GetValueOrDefault(loserNextMatchId);
    }
}
