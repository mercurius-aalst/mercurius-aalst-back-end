using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Mercurius.LAN.API.DTOs.Auth;
using Mercurius.LAN.API.Options;
using Microsoft.Extensions.Options;

namespace Mercurius.LAN.API.Services.Auth0;

public sealed class Auth0ManagementService : IAuth0ManagementService
{
    private readonly HttpClient _httpClient;
    private readonly Auth0ManagementOptions _options;
    private readonly ILogger<Auth0ManagementService> _logger;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _managementToken;
    private DateTimeOffset _managementTokenExpiresAtUtc;

    public Auth0ManagementService(
        HttpClient httpClient,
        IOptions<Auth0ManagementOptions> options,
        ILogger<Auth0ManagementService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Auth0ProfileSnapshot> GetUserProfileAsync(string auth0UserId, CancellationToken cancellationToken = default)
    {
        if (!_options.HasManagementApiConfiguration)
            return new Auth0ProfileSnapshot(null, null, false);

        using var request = new HttpRequestMessage(HttpMethod.Get, BuildUri($"api/v2/users/{Uri.EscapeDataString(auth0UserId)}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetManagementTokenAsync(cancellationToken));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Auth0 profile sync failed for user {Auth0UserId} with status {StatusCode}.", auth0UserId, response.StatusCode);
            return new Auth0ProfileSnapshot(null, null, false);
        }

        var profile = await response.Content.ReadFromJsonAsync<Auth0UserResponse>(cancellationToken);
        var hasPasswordResetIdentity = profile?.Identities?.Any(identity =>
            !string.IsNullOrWhiteSpace(identity.Connection) &&
            string.Equals(identity.Connection, _options.DatabaseConnection, StringComparison.OrdinalIgnoreCase)) == true;

        return new Auth0ProfileSnapshot(profile?.Email, profile?.EmailVerified, hasPasswordResetIdentity);
    }

    public async Task SendVerificationEmailAsync(string auth0UserId, CancellationToken cancellationToken = default)
    {
        if (!_options.HasManagementApiConfiguration)
        {
            _logger.LogWarning("Auth0 verification email was requested, but Management API configuration is incomplete.");
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildUri("api/v2/jobs/verification-email"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetManagementTokenAsync(cancellationToken));
        request.Content = JsonContent.Create(new VerificationEmailRequest(auth0UserId));

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task SendPasswordResetEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (!_options.HasPasswordResetConfiguration)
        {
            _logger.LogWarning("Auth0 password reset was requested, but password reset configuration is incomplete.");
            return;
        }

        using var response = await _httpClient.PostAsJsonAsync(
            BuildUri("dbconnections/change_password"),
            new PasswordResetRequest(
                _options.PasswordResetClientId!,
                email,
                _options.DatabaseConnection!),
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    private async Task<string> GetManagementTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_managementToken) &&
            _managementTokenExpiresAtUtc > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            return _managementToken;
        }

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(_managementToken) &&
                _managementTokenExpiresAtUtc > DateTimeOffset.UtcNow.AddMinutes(1))
            {
                return _managementToken;
            }

            using var response = await _httpClient.PostAsJsonAsync(
                BuildUri("oauth/token"),
                new ManagementTokenRequest(
                    _options.ManagementClientId!,
                    _options.ManagementClientSecret!,
                    _options.ManagementAudience!,
                    "client_credentials"),
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadFromJsonAsync<ManagementTokenResponse>(cancellationToken);
            _managementToken = token?.AccessToken ?? throw new InvalidOperationException("Auth0 did not return a management access token.");
            _managementTokenExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, token.ExpiresIn - 60));

            return _managementToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private Uri BuildUri(string path)
    {
        return new Uri(new Uri(_options.AuthorityBaseUri), path);
    }

    private sealed record Auth0UserResponse(
        [property: JsonPropertyName("email")] string? Email,
        [property: JsonPropertyName("email_verified")] bool? EmailVerified,
        [property: JsonPropertyName("identities")] IReadOnlyList<Auth0IdentityResponse>? Identities);

    private sealed record Auth0IdentityResponse(
        [property: JsonPropertyName("provider")] string? Provider,
        [property: JsonPropertyName("connection")] string? Connection);

    private sealed record VerificationEmailRequest(
        [property: JsonPropertyName("user_id")] string UserId);

    private sealed record PasswordResetRequest(
        [property: JsonPropertyName("client_id")] string ClientId,
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("connection")] string Connection);

    private sealed record ManagementTokenRequest(
        [property: JsonPropertyName("client_id")] string ClientId,
        [property: JsonPropertyName("client_secret")] string ClientSecret,
        [property: JsonPropertyName("audience")] string Audience,
        [property: JsonPropertyName("grant_type")] string GrantType);

    private sealed record ManagementTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
