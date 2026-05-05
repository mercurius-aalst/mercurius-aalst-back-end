using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Auth.Module.Exceptions;
using Auth.Module.Models;
using Auth.Module.Persistence;
using Auth.Module.Services.Token;
using Auth.Module.Services.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.WebUtilities;

namespace Auth.Module.Services.External;

public class ExternalAuthService : IExternalAuthService
{
    private const string GoogleProvider = "google";
    private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(10);

    private readonly IAuthDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly IAuthUserService _authUserService;
    private readonly IExternalUserProfileProvisioner _profileProvisioner;
    private readonly IOidcStateStore _stateStore;
    private readonly HttpClient _httpClient;
    private readonly GoogleOidcOptions _options;
    private readonly ILogger<ExternalAuthService> _logger;

    public ExternalAuthService(
        IAuthDbContext dbContext,
        ITokenService tokenService,
        IAuthUserService authUserService,
        IExternalUserProfileProvisioner profileProvisioner,
        IOidcStateStore stateStore,
        HttpClient httpClient,
        IOptions<GoogleOidcOptions> options,
        ILogger<ExternalAuthService> logger)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
        _authUserService = authUserService;
        _profileProvisioner = profileProvisioner;
        _stateStore = stateStore;
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<GoogleAuthStartResponse> StartGoogleAuthAsync(CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var state = CreateRandomToken(32);
        var nonce = CreateRandomToken(32);
        var codeVerifier = CreateRandomToken(64);
        var codeChallenge = Base64UrlEncoder.Encode(SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier)));

        await _stateStore.StoreAsync(new OidcStateEntry
        {
            State = state,
            Nonce = nonce,
            CodeVerifier = codeVerifier,
            ExpiresAtUtc = DateTime.UtcNow.Add(StateLifetime)
        }, cancellationToken);

        var authorizationUrl = QueryHelpers.AddQueryString(
            "https://accounts.google.com/o/oauth2/v2/auth",
            new Dictionary<string, string?>
            {
                ["client_id"] = _options.ClientId,
                ["redirect_uri"] = _options.RedirectUri,
                ["response_type"] = "code",
                ["scope"] = "openid email profile",
                ["state"] = state,
                ["nonce"] = nonce,
                ["code_challenge"] = codeChallenge,
                ["code_challenge_method"] = "S256",
                ["access_type"] = "offline",
                ["prompt"] = "consent"
            });

        return new GoogleAuthStartResponse
        {
            AuthorizationUrl = authorizationUrl
        };
    }

    public async Task<AuthTokenResponse> CompleteGoogleAuthAsync(GoogleAuthCallbackRequest request, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var state = await _stateStore.TakeAsync(request.State, cancellationToken);
        if (state == null)
        {
            _logger.LogWarning("External Google login failed due to invalid or expired state.");
            throw new ValidationException("Invalid or expired external authentication state.");
        }

        var tokenResponse = await ExchangeCodeAsync(request.Code, state.CodeVerifier, cancellationToken);
        var principal = await ValidateIdTokenAsync(tokenResponse.IdToken, state.Nonce, cancellationToken);

        var providerSubject = principal.FindFirst("sub")?.Value;
        var email = principal.FindFirst("email")?.Value;
        var emailVerified = bool.TryParse(principal.FindFirst("email_verified")?.Value, out var verified) && verified;
        var givenName = principal.FindFirst("given_name")?.Value;
        var familyName = principal.FindFirst("family_name")?.Value;

        if (string.IsNullOrWhiteSpace(providerSubject))
        {
            _logger.LogWarning("External Google login failed because no subject identifier was returned.");
            throw new ValidationException("Google did not provide a subject identifier.");
        }

        var authUser = await _dbContext.AuthUsers
            .Include(user => user.Roles)
            .Include(user => user.RefreshTokens)
            .Include(user => user.ExternalIdentities)
            .FirstOrDefaultAsync(
                user => user.ExternalIdentities.Any(identity => identity.Provider == GoogleProvider && identity.ProviderSubject == providerSubject),
                cancellationToken);

        if (authUser == null && emailVerified && !string.IsNullOrWhiteSpace(email))
        {
            var existingUserId = await _profileProvisioner.FindUserIdByVerifiedEmailAsync(email, cancellationToken);
            if (existingUserId.HasValue)
            {
                authUser = await _dbContext.AuthUsers
                    .Include(user => user.Roles)
                    .Include(user => user.RefreshTokens)
                    .Include(user => user.ExternalIdentities)
                    .FirstOrDefaultAsync(user => user.Id == existingUserId.Value, cancellationToken)
                    ?? throw new NotFoundException($"Auth user with ID {existingUserId.Value} not found.");
            }
        }

        if (authUser == null)
        {
            if (!emailVerified || string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("External Google login failed because no verified email was provided.");
                throw new ValidationException("Google sign-in requires a verified email address.");
            }

            var username = await GenerateUniqueUsernameAsync(email, givenName, familyName);
            var userId = await _authUserService.CreateExternalAuthUserAsync(username);
            await _profileProvisioner.CreateMinimalProfileAsync(userId, username, email, givenName, familyName, cancellationToken);
            _logger.LogInformation("Provisioned new external auth user {UserId} for Google login.", userId);

            authUser = await _dbContext.AuthUsers
                .Include(user => user.Roles)
                .Include(user => user.RefreshTokens)
                .Include(user => user.ExternalIdentities)
                .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken)
                ?? throw new NotFoundException($"Auth user with ID {userId} not found.");
        }

        var externalIdentity = authUser.ExternalIdentities.FirstOrDefault(identity => identity.Provider == GoogleProvider && identity.ProviderSubject == providerSubject);
        if (externalIdentity == null)
        {
            externalIdentity = new ExternalIdentity
            {
                Provider = GoogleProvider,
                ProviderSubject = providerSubject,
                Email = email,
                EmailVerified = emailVerified,
                LinkedAtUtc = DateTime.UtcNow,
                LastLoginAtUtc = DateTime.UtcNow,
                UserId = authUser.Id
            };

            authUser.ExternalIdentities.Add(externalIdentity);
        }
        else
        {
            externalIdentity.Email = email;
            externalIdentity.EmailVerified = emailVerified;
            externalIdentity.LastLoginAtUtc = DateTime.UtcNow;
        }

        var refreshToken = _tokenService.GenerateRefreshToken(authUser.Id);
        authUser.RefreshTokens.Add(refreshToken);

        foreach (var expiredToken in authUser.RefreshTokens.Where(token => token.Expires < DateTime.UtcNow).ToList())
        {
            authUser.RefreshTokens.Remove(expiredToken);
            _dbContext.RefreshTokens.Remove(expiredToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("External Google login succeeded for auth user {UserId}.", authUser.Id);

        return new AuthTokenResponse
        {
            Token = _tokenService.GenerateJwtToken(authUser),
            RefreshToken = refreshToken.Token
        };
    }

    public Task<GoogleAuthStartResponse> StartGoogleLinkAsync(CancellationToken cancellationToken = default) =>
        StartGoogleAuthAsync(cancellationToken);

    public async Task CompleteGoogleLinkAsync(Guid userId, GoogleAuthCallbackRequest request, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var state = await _stateStore.TakeAsync(request.State, cancellationToken);
        if (state == null)
        {
            _logger.LogWarning("External Google link failed due to invalid or expired state for auth user {UserId}.", userId);
            throw new ValidationException("Invalid or expired external authentication state.");
        }

        var tokenResponse = await ExchangeCodeAsync(request.Code, state.CodeVerifier, cancellationToken);
        var principal = await ValidateIdTokenAsync(tokenResponse.IdToken, state.Nonce, cancellationToken);

        var providerSubject = principal.FindFirst("sub")?.Value;
        var email = principal.FindFirst("email")?.Value;
        var emailVerified = bool.TryParse(principal.FindFirst("email_verified")?.Value, out var verified) && verified;

        if (string.IsNullOrWhiteSpace(providerSubject))
        {
            _logger.LogWarning("External Google link failed because no subject identifier was returned for auth user {UserId}.", userId);
            throw new ValidationException("Google did not provide a subject identifier.");
        }

        var authUser = await _dbContext.AuthUsers
            .Include(user => user.ExternalIdentities)
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken)
            ?? throw new NotFoundException($"User with ID {userId} not found.");

        var existingIdentity = await _dbContext.ExternalIdentities
            .FirstOrDefaultAsync(
                identity => identity.Provider == GoogleProvider && identity.ProviderSubject == providerSubject,
                cancellationToken);

        if (existingIdentity != null && existingIdentity.UserId != userId)
        {
            _logger.LogWarning("External Google link rejected because provider subject is already linked to another user.");
            throw new ValidationException("This Google account is already linked to another user.");
        }

        if (existingIdentity == null)
        {
            authUser.ExternalIdentities.Add(new ExternalIdentity
            {
                Provider = GoogleProvider,
                ProviderSubject = providerSubject,
                Email = email,
                EmailVerified = emailVerified,
                LinkedAtUtc = DateTime.UtcNow,
                LastLoginAtUtc = DateTime.UtcNow,
                UserId = userId
            });
        }
        else
        {
            existingIdentity.Email = email;
            existingIdentity.EmailVerified = emailVerified;
            existingIdentity.LastLoginAtUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("External Google account linked for auth user {UserId}.", userId);
    }

    public async Task UnlinkExternalIdentityAsync(Guid userId, string provider, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(provider, GoogleProvider, StringComparison.OrdinalIgnoreCase))
            throw new NotFoundException($"Provider '{provider}' is not supported.");

        var authUser = await _dbContext.AuthUsers
            .Include(user => user.ExternalIdentities)
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken)
            ?? throw new NotFoundException($"User with ID {userId} not found.");

        var identity = authUser.ExternalIdentities.FirstOrDefault(externalIdentity => externalIdentity.Provider == GoogleProvider)
            ?? throw new NotFoundException($"Provider '{provider}' is not linked to the current user.");

        var hasLocalPassword = authUser.PasswordHash is { Length: > 0 } && authUser.Salt is { Length: > 0 };
        var hasAnotherExternalIdentity = authUser.ExternalIdentities.Any(externalIdentity => externalIdentity.Id != identity.Id);

        if (!hasLocalPassword && !hasAnotherExternalIdentity)
        {
            _logger.LogWarning("External unlink rejected for auth user {UserId} because it would remove the last sign-in method.", userId);
            throw new ValidationException("Cannot unlink the last available sign-in method.");
        }

        authUser.ExternalIdentities.Remove(identity);
        _dbContext.ExternalIdentities.Remove(identity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("External provider {Provider} unlinked for auth user {UserId}.", provider, userId);
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId) ||
            string.IsNullOrWhiteSpace(_options.ClientSecret) ||
            string.IsNullOrWhiteSpace(_options.RedirectUri))
        {
            throw new ValidationException("Google external authentication is not configured.");
        }
    }

    private async Task<GoogleTokenResponse> ExchangeCodeAsync(string code, string codeVerifier, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsync(
            "https://oauth2.googleapis.com/token",
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
            throw new InvalidCredentialsException("Google authorization code exchange failed.");

        var tokenResponse = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken: cancellationToken);
        if (tokenResponse == null || string.IsNullOrWhiteSpace(tokenResponse.IdToken))
            throw new InvalidCredentialsException("Google token response did not contain an ID token.");

        return tokenResponse;
    }

    private async Task<System.Security.Claims.ClaimsPrincipal> ValidateIdTokenAsync(string idToken, string expectedNonce, CancellationToken cancellationToken)
    {
        var configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            _options.MetadataAddress,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever { RequireHttps = true });

        var configuration = await configurationManager.GetConfigurationAsync(cancellationToken);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = [configuration.Issuer, "https://accounts.google.com", "accounts.google.com"],
            ValidateAudience = true,
            ValidAudience = _options.ClientId,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = configuration.SigningKeys,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        var handler = new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(idToken, validationParameters, out _);

        var nonce = principal.FindFirst("nonce")?.Value;
        if (!string.Equals(nonce, expectedNonce, StringComparison.Ordinal))
            throw new InvalidCredentialsException("Google ID token nonce validation failed.");

        return principal;
    }

    private async Task<string> GenerateUniqueUsernameAsync(string email, string? givenName, string? familyName)
    {
        var baseCandidate = BuildUsernameCandidate(email, givenName, familyName);

        for (var suffix = 0; suffix < 1000; suffix++)
        {
            var candidate = suffix == 0 ? baseCandidate : AppendNumericSuffix(baseCandidate, suffix);

            if (!await _dbContext.AuthUsers.AnyAsync(user => user.Username == candidate))
                return candidate;
        }

        throw new ValidationException("Unable to generate a unique username for the external account.");
    }

    private static string BuildUsernameCandidate(string email, string? givenName, string? familyName)
    {
        var candidates = new[]
        {
            email.Split('@')[0],
            $"{givenName}{familyName}",
            givenName,
            "user"
        };

        foreach (var candidate in candidates)
        {
            var sanitized = new string((candidate ?? string.Empty).Where(char.IsLetterOrDigit).ToArray());
            if (sanitized.Length >= 3)
                return sanitized[..Math.Min(32, sanitized.Length)];
        }

        return "user";
    }

    private static string AppendNumericSuffix(string baseCandidate, int suffix)
    {
        var suffixText = suffix.ToString();
        var maxBaseLength = Math.Max(3, 32 - suffixText.Length);
        var trimmedBase = baseCandidate[..Math.Min(baseCandidate.Length, maxBaseLength)];
        return $"{trimmedBase}{suffixText}";
    }

    private static string CreateRandomToken(int byteLength)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        return Base64UrlEncoder.Encode(bytes);
    }

    private sealed class GoogleTokenResponse
    {
        [JsonPropertyName("id_token")]
        public string IdToken { get; set; } = string.Empty;
    }
}
