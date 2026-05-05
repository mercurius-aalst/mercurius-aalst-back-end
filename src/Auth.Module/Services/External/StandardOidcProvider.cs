using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Auth.Module.Exceptions;
using Auth.Module.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Module.Services.External;

public class StandardOidcProvider : IOidcProviderStrategy
{
    private readonly HttpClient _httpClient;
    private readonly OidcProviderOptions _options;
    private readonly IConfigurationManager<OpenIdConnectConfiguration> _configurationManager;

    public StandardOidcProvider(
        string providerName,
        OidcProviderOptions options,
        HttpClient httpClient,
        IConfigurationManager<OpenIdConnectConfiguration>? configurationManager = null)
    {
        ProviderName = NormalizeProviderName(providerName);
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _configurationManager = configurationManager ?? CreateConfigurationManager(options);
    }

    public string ProviderName { get; }

    public async Task<OidcAuthStartResponse> CreateAuthorizationResponseAsync(OidcStateEntry state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        EnsureConfigured();

        var configuration = await _configurationManager.GetConfigurationAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(configuration.AuthorizationEndpoint))
            throw new ValidationException($"OIDC provider '{ProviderName}' did not expose an authorization endpoint.");

        var codeChallenge = Base64UrlEncoder.Encode(SHA256.HashData(Encoding.UTF8.GetBytes(state.CodeVerifier)));
        var queryParameters = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["client_id"] = _options.ClientId,
            ["redirect_uri"] = _options.RedirectUri,
            ["response_type"] = "code",
            ["scope"] = string.Join(" ", _options.Scopes.Distinct(StringComparer.OrdinalIgnoreCase)),
            ["state"] = state.State,
            ["nonce"] = state.Nonce,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256"
        };

        foreach (var parameter in _options.AdditionalAuthorizationParameters)
        {
            queryParameters[parameter.Key] = parameter.Value;
        }

        return new OidcAuthStartResponse
        {
            AuthorizationUrl = QueryHelpers.AddQueryString(configuration.AuthorizationEndpoint, queryParameters)
        };
    }

    public async Task<OidcTokenResponse> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var configuration = await _configurationManager.GetConfigurationAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(configuration.TokenEndpoint))
            throw new ValidationException($"OIDC provider '{ProviderName}' did not expose a token endpoint.");

        var response = await _httpClient.PostAsync(
            configuration.TokenEndpoint,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["redirect_uri"] = _options.RedirectUri,
                ["grant_type"] = "authorization_code",
                ["code_verifier"] = codeVerifier
            }),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidCredentialsException($"{ProviderName} authorization code exchange failed.");

        var tokenResponse = await response.Content.ReadFromJsonAsync<OidcTokenResponse>(cancellationToken: cancellationToken);
        if (tokenResponse == null || string.IsNullOrWhiteSpace(tokenResponse.IdToken))
            throw new InvalidCredentialsException($"{ProviderName} token response did not contain an ID token.");

        return tokenResponse;
    }

    public async Task<OidcUserInfo> ValidateAndExtractClaimsAsync(string idToken, string expectedNonce, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var configuration = await _configurationManager.GetConfigurationAsync(cancellationToken);
        var validIssuers = new[] { configuration.Issuer }
            .Concat(_options.ValidIssuers)
            .Where(issuer => !string.IsNullOrWhiteSpace(issuer))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = validIssuers,
            ValidateAudience = true,
            ValidAudience = _options.ClientId,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = configuration.SigningKeys,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(idToken, validationParameters, out _);

        var nonce = FindClaim(principal, "nonce");
        if (!string.Equals(nonce, expectedNonce, StringComparison.Ordinal))
            throw new InvalidCredentialsException($"{ProviderName} ID token nonce validation failed.");

        var subject = FindClaim(principal, "sub", ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(subject))
            throw new ValidationException($"{ProviderName} did not provide a subject identifier.");

        var emailVerified = bool.TryParse(FindClaim(principal, "email_verified"), out var verified) && verified;

        return new OidcUserInfo(
            subject,
            FindClaim(principal, "email", ClaimTypes.Email),
            emailVerified,
            FindClaim(principal, "given_name", ClaimTypes.GivenName),
            FindClaim(principal, "family_name", ClaimTypes.Surname));
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId) ||
            string.IsNullOrWhiteSpace(_options.ClientSecret) ||
            string.IsNullOrWhiteSpace(_options.RedirectUri) ||
            string.IsNullOrWhiteSpace(_options.MetadataAddress))
        {
            throw new ValidationException($"OIDC provider '{ProviderName}' is not fully configured.");
        }
    }

    private static IConfigurationManager<OpenIdConnectConfiguration> CreateConfigurationManager(OidcProviderOptions options) =>
        new ConfigurationManager<OpenIdConnectConfiguration>(
            options.MetadataAddress,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever { RequireHttps = true });

    private static string NormalizeProviderName(string providerName) => providerName.Trim().ToLowerInvariant();

    private static string? FindClaim(ClaimsPrincipal principal, params string[] claimTypes) =>
        claimTypes
            .Select(type => principal.FindFirst(type)?.Value)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}
