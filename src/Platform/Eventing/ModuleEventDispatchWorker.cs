using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Platform.Eventing;

internal sealed class ModuleEventDispatchWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ModuleEventingOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ModuleEventDispatchWorker> _logger;

    public ModuleEventDispatchWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<ModuleEventingOptions> options,
        TimeProvider timeProvider,
        ILogger<ModuleEventDispatchWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var nextCleanupAtUtc = DateTimeOffset.MinValue;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<IModuleEventDispatcher>();
                var processed = await dispatcher.DispatchPendingAsync(
                    _options.DispatchBatchSize,
                    stoppingToken);

                var now = _timeProvider.GetUtcNow();
                if (now >= nextCleanupAtUtc)
                {
                    await dispatcher.CleanupTerminalAsync(stoppingToken);
                    nextCleanupAtUtc = now + _options.CleanupInterval;
                }

                if (processed > 0)
                    continue;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Module event dispatch worker failed.");
            }

            await Task.Delay(_options.PollInterval, _timeProvider, stoppingToken);
        }
    }
}
