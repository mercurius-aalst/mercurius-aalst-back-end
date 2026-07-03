namespace Platform.Eventing;

public interface IModuleEventDispatcher
{
    Task<int> DispatchPendingAsync(int batchSize = 50, CancellationToken cancellationToken = default);
}
