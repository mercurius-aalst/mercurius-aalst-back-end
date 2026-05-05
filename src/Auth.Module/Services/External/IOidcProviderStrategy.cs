using Auth.Module.Models;

namespace Auth.Module.Services.External;

public interface IOidcProviderStrategy
{
    string ProviderName { get; }
    Task<OidcAuthStartResponse> CreateAuthorizationResponseAsync(OidcStateEntry state, CancellationToken cancellationToken = default);
    Task<OidcTokenResponse> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken cancellationToken = default);
    Task<OidcUserInfo> ValidateAndExtractClaimsAsync(string idToken, string expectedNonce, CancellationToken cancellationToken = default);
}
