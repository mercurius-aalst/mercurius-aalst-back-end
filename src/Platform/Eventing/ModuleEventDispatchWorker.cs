using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Platform.Eventing;

internal sealed class ModuleEventDispatchWorker : BackgroundService
{
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(2);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ModuleEventDispatchWorker> _logger;

    public ModuleEventDispatchWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<ModuleEventDispatchWorker> logger)
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
                var dispatcher = scope.ServiceProvider.GetRequiredService<IModuleEventDispatcher>();
                if (await dispatcher.DispatchPendingAsync(cancellationToken: stoppingToken) > 0)
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

            await Task.Delay(IdleDelay, stoppingToken);
        }
    }
}
