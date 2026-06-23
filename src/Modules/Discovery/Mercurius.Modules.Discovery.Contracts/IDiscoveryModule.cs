namespace Mercurius.Modules.Discovery.Contracts;

public interface IDiscoveryModule
{
    Task<DiscoverySearchResponse> SearchAsync(
        DiscoverySearchRequest request,
        CancellationToken cancellationToken = default);
}
