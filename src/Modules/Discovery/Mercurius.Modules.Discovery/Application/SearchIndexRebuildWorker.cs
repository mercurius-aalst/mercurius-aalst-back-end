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
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var ownership = scope.ServiceProvider.GetRequiredService<DiscoveryRebuildOwnership>();
                var rebuildService = scope.ServiceProvider.GetRequiredService<SearchIndexRebuildService>();
                await using var lease = await ownership.TryAcquireAsync(stoppingToken);
                if (lease is not null)
                {
                    await rebuildService.RecoverInterruptedJobsAsync(stoppingToken);
                    await rebuildService.EnsureInitialJobAsync(stoppingToken);
                    await rebuildService.RunNextAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Discovery search-index rebuild ownership or work failed.");
            }

            try
            {
                await Task.Delay(IdleDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
