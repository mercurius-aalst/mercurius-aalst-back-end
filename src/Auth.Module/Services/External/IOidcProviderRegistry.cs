namespace Auth.Module.Services.External;

public interface IOidcProviderRegistry
{
    IOidcProviderStrategy? GetProvider(string providerName);
    IReadOnlyCollection<string> GetEnabledProviders();
}
