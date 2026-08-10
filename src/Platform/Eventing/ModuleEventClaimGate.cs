namespace Platform.Eventing;

internal sealed class ModuleEventClaimGate : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public Task EnterAsync(CancellationToken cancellationToken) =>
        _semaphore.WaitAsync(cancellationToken);

    public void Exit() => _semaphore.Release();

    public void Dispose() => _semaphore.Dispose();
}
