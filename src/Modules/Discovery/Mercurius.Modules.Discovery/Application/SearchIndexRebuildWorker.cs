using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Mercurius.Modules.Discovery.Application;

internal sealed class SearchIndexRebuildWorker : BackgroundService
{
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(2);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SearchIndexRebuildWorker> _logger;

    public SearchIndexRebuildWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<SearchIndexRebuildWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var initialScope = _scopeFactory.CreateScope();
            var rebuildService = initialScope.ServiceProvider.GetRequiredService<SearchIndexRebuildService>();
            await rebuildService.RecoverInterruptedJobsAsync(stoppingToken);
            await rebuildService.EnsureInitialJobAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Discovery search-index initial rebuild scheduling failed.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var rebuildService = scope.ServiceProvider.GetRequiredService<SearchIndexRebuildService>();
                if (await rebuildService.RunNextAsync(stoppingToken))
                    continue;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Discovery search-index rebuild worker failed.");
            }

            await Task.Delay(IdleDelay, stoppingToken);
        }
    }
}
