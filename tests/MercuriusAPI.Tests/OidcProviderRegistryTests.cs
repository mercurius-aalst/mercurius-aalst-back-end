using Auth.Module.Models;
using Auth.Module.Services.External;

namespace Mercurius.LAN.API.Tests;

public class OidcProviderRegistryTests
{
    [Fact]
    public void GetProvider_ReturnsProvider_CaseInsensitive()
    {
        var registry = new OidcProviderRegistry([new FakeOidcProviderStrategy("google")]);

        var provider = registry.GetProvider("Google");

        Assert.NotNull(provider);
        Assert.Equal("google", provider!.ProviderName);
    }

    [Fact]
    public void GetProvider_ReturnsNull_WhenProviderIsUnknown()
    {
        var registry = new OidcProviderRegistry([new FakeOidcProviderStrategy("google")]);

        var provider = registry.GetProvider("microsoft");

        Assert.Null(provider);
    }

    [Fact]
    public void GetEnabledProviders_ReturnsRegisteredProviders()
    {
        var registry = new OidcProviderRegistry(
        [
            new FakeOidcProviderStrategy("google"),
            new FakeOidcProviderStrategy("microsoft")
        ]);

        var providers = registry.GetEnabledProviders();

        Assert.Equal(["google", "microsoft"], providers.OrderBy(provider => provider).ToArray());
    }

    private sealed class FakeOidcProviderStrategy(string providerName) : IOidcProviderStrategy
    {
        public string ProviderName { get; } = providerName;

        public Task<OidcAuthStartResponse> CreateAuthorizationResponseAsync(OidcStateEntry state, CancellationToken cancellationToken = default) =>
            Task.FromResult(new OidcAuthStartResponse());

        public Task<OidcTokenResponse> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken cancellationToken = default) =>
            Task.FromResult(new OidcTokenResponse { IdToken = "token" });

        public Task<OidcUserInfo> ValidateAndExtractClaimsAsync(string idToken, string expectedNonce, CancellationToken cancellationToken = default) =>
            Task.FromResult(new OidcUserInfo("subject", "user@example.com", true, "Given", "Family"));
    }
}
