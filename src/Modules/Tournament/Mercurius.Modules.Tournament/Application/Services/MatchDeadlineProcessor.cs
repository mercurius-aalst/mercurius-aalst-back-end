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
        var tournaments = await dbContext.Tournaments
            .Include(tournament => tournament.Matches)
            .Where(tournament => tournament.Status == TournamentStatus.InProgress)
            .ToListAsync(cancellationToken);

        var changed = false;
        foreach (var tournament in tournaments)
        {
            var matchesById = tournament.Matches.ToDictionary(match => match.Id);
            foreach (var match in tournament.Matches)
            {
                if (match.WinnerNextMatchId.HasValue)
                    match.WinnerNextMatch = matchesById.GetValueOrDefault(match.WinnerNextMatchId.Value);
                if (match.LoserNextMatchId.HasValue)
                    match.LoserNextMatch = matchesById.GetValueOrDefault(match.LoserNextMatchId.Value);

                var beforeState = match.LifecycleState;
                var beforeResult = match.HasResult;
                match.ApplyDeadline(nowUtc);
                if (beforeState == match.LifecycleState && beforeResult == match.HasResult)
                    continue;

                changed = true;
                if (!beforeResult && match.HasResult && match.GetWinnerId() is Guid winnerId)
                {
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
        }

        if (!changed)
            return;

        await using var transaction = dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory"
            ? null
            : await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }
}
