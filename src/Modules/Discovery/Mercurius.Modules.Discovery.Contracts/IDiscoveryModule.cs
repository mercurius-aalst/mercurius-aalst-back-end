namespace Mercurius.Modules.Discovery.Contracts;

public interface IDiscoveryModule
{
    Task<DiscoverySearchResponse> SearchAsync(
        DiscoverySearchRequest request,
        CancellationToken cancellationToken = default);

    Task<DiscoverySearchIndexRebuildJob> CreateSearchIndexRebuildJobAsync(
        CancellationToken cancellationToken = default);

    Task<DiscoverySearchIndexRebuildJob?> GetSearchIndexRebuildJobAsync(
        Guid jobId,
        CancellationToken cancellationToken = default);
}
