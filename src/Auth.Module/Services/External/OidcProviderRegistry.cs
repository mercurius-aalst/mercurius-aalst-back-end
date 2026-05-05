namespace Auth.Module.Services.External;

public class OidcProviderRegistry : IOidcProviderRegistry
{
    private readonly IReadOnlyDictionary<string, IOidcProviderStrategy> _providers;

    public OidcProviderRegistry(IEnumerable<IOidcProviderStrategy> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        var providerDictionary = new Dictionary<string, IOidcProviderStrategy>(StringComparer.OrdinalIgnoreCase);
        foreach (var provider in providers)
        {
            if (!providerDictionary.TryAdd(provider.ProviderName, provider))
                throw new InvalidOperationException($"OIDC provider '{provider.ProviderName}' is registered more than once.");
        }

        _providers = providerDictionary;
    }

    public IOidcProviderStrategy? GetProvider(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            return null;

        _providers.TryGetValue(providerName, out var provider);
        return provider;
    }

    public IReadOnlyCollection<string> GetEnabledProviders() => _providers.Keys.ToArray();
}
