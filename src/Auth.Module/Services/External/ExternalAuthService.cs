using System.Security.Cryptography;
using Auth.Module.Exceptions;
using Auth.Module.Models;
using Auth.Module.Persistence;
using Auth.Module.Services.Token;
using Auth.Module.Services.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Auth.Module.Services.External;

public class ExternalAuthService : IExternalAuthService
{
    private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(10);

    private readonly IAuthDbContext _dbContext;
    private readonly ITokenService _tokenService;
    private readonly IAuthUserService _authUserService;
    private readonly IExternalUserProfileProvisioner _profileProvisioner;
    private readonly IOidcProviderRegistry _providerRegistry;
    private readonly IOidcStateStore _stateStore;
    private readonly ILogger<ExternalAuthService> _logger;

    public ExternalAuthService(
        IAuthDbContext dbContext,
        ITokenService tokenService,
        IAuthUserService authUserService,
        IExternalUserProfileProvisioner profileProvisioner,
        IOidcProviderRegistry providerRegistry,
        IOidcStateStore stateStore,
        ILogger<ExternalAuthService> logger)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
        _authUserService = authUserService;
        _profileProvisioner = profileProvisioner;
        _providerRegistry = providerRegistry;
        _stateStore = stateStore;
        _logger = logger;
    }

    public async Task<OidcAuthStartResponse> StartAuthAsync(string provider, CancellationToken cancellationToken = default)
    {
        var oidcProvider = GetRequiredProvider(provider);

        var state = CreateRandomToken(32);
        var nonce = CreateRandomToken(32);
        var codeVerifier = CreateRandomToken(64);

        var stateEntry = new OidcStateEntry
        {
            Provider = oidcProvider.ProviderName,
            State = state,
            Nonce = nonce,
            CodeVerifier = codeVerifier,
            ExpiresAtUtc = DateTime.UtcNow.Add(StateLifetime)
        };

        await _stateStore.StoreAsync(stateEntry, cancellationToken);
        return await oidcProvider.CreateAuthorizationResponseAsync(stateEntry, cancellationToken);
    }

    public async Task<AuthTokenResponse> CompleteAuthAsync(string provider, OidcCallbackRequest request, CancellationToken cancellationToken = default)
    {
        var oidcProvider = GetRequiredProvider(provider);
        var state = await TakeStateAsync(oidcProvider.ProviderName, request.State, "login", cancellationToken);
        var tokenResponse = await oidcProvider.ExchangeCodeAsync(request.Code, state.CodeVerifier, cancellationToken);
        var userInfo = await oidcProvider.ValidateAndExtractClaimsAsync(tokenResponse.IdToken, state.Nonce, cancellationToken);

        var authUser = await _dbContext.AuthUsers
            .Include(user => user.Roles)
            .Include(user => user.RefreshTokens)
            .Include(user => user.ExternalIdentities)
            .FirstOrDefaultAsync(
                user => user.ExternalIdentities.Any(identity => identity.Provider == oidcProvider.ProviderName && identity.ProviderSubject == userInfo.Subject),
                cancellationToken);

        if (authUser == null && userInfo.EmailVerified && !string.IsNullOrWhiteSpace(userInfo.Email))
        {
            var existingUserId = await _profileProvisioner.FindUserIdByVerifiedEmailAsync(userInfo.Email, cancellationToken);
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
            if (!userInfo.EmailVerified || string.IsNullOrWhiteSpace(userInfo.Email))
            {
                _logger.LogWarning("External provider {Provider} login failed because no verified email was provided.", oidcProvider.ProviderName);
                throw new ValidationException("External sign-in requires a verified email address.");
            }

            var username = await GenerateUniqueUsernameAsync(userInfo.Email, userInfo.GivenName, userInfo.FamilyName);
            var userId = await _authUserService.CreateExternalAuthUserAsync(username);
            await _profileProvisioner.CreateMinimalProfileAsync(userId, username, userInfo.Email, userInfo.GivenName, userInfo.FamilyName, cancellationToken);
            _logger.LogInformation("Provisioned new external auth user {UserId} for provider {Provider} login.", userId, oidcProvider.ProviderName);

            authUser = await _dbContext.AuthUsers
                .Include(user => user.Roles)
                .Include(user => user.RefreshTokens)
                .Include(user => user.ExternalIdentities)
                .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken)
                ?? throw new NotFoundException($"Auth user with ID {userId} not found.");
        }

        var externalIdentity = authUser.ExternalIdentities.FirstOrDefault(identity => identity.Provider == oidcProvider.ProviderName && identity.ProviderSubject == userInfo.Subject);
        if (externalIdentity == null)
        {
            externalIdentity = new ExternalIdentity
            {
                Provider = oidcProvider.ProviderName,
                ProviderSubject = userInfo.Subject,
                Email = userInfo.Email,
                EmailVerified = userInfo.EmailVerified,
                LinkedAtUtc = DateTime.UtcNow,
                LastLoginAtUtc = DateTime.UtcNow,
                UserId = authUser.Id
            };

            authUser.ExternalIdentities.Add(externalIdentity);
        }
        else
        {
            externalIdentity.Email = userInfo.Email;
            externalIdentity.EmailVerified = userInfo.EmailVerified;
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
        _logger.LogInformation("External provider {Provider} login succeeded for auth user {UserId}.", oidcProvider.ProviderName, authUser.Id);

        return new AuthTokenResponse
        {
            Token = _tokenService.GenerateJwtToken(authUser),
            RefreshToken = refreshToken.Token
        };
    }

    public Task<OidcAuthStartResponse> StartLinkAsync(string provider, CancellationToken cancellationToken = default) =>
        StartAuthAsync(provider, cancellationToken);

    public async Task CompleteLinkAsync(string provider, Guid userId, OidcCallbackRequest request, CancellationToken cancellationToken = default)
    {
        var oidcProvider = GetRequiredProvider(provider);
        var state = await TakeStateAsync(oidcProvider.ProviderName, request.State, "link", cancellationToken);
        var tokenResponse = await oidcProvider.ExchangeCodeAsync(request.Code, state.CodeVerifier, cancellationToken);
        var userInfo = await oidcProvider.ValidateAndExtractClaimsAsync(tokenResponse.IdToken, state.Nonce, cancellationToken);

        var authUser = await _dbContext.AuthUsers
            .Include(user => user.ExternalIdentities)
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken)
            ?? throw new NotFoundException($"User with ID {userId} not found.");

        var existingIdentity = await _dbContext.ExternalIdentities
            .FirstOrDefaultAsync(
                identity => identity.Provider == oidcProvider.ProviderName && identity.ProviderSubject == userInfo.Subject,
                cancellationToken);

        if (existingIdentity != null && existingIdentity.UserId != userId)
        {
            _logger.LogWarning("External provider {Provider} link rejected because the provider subject is already linked to another user.", oidcProvider.ProviderName);
            throw new ValidationException("This external account is already linked to another user.");
        }

        if (existingIdentity == null)
        {
            authUser.ExternalIdentities.Add(new ExternalIdentity
            {
                Provider = oidcProvider.ProviderName,
                ProviderSubject = userInfo.Subject,
                Email = userInfo.Email,
                EmailVerified = userInfo.EmailVerified,
                LinkedAtUtc = DateTime.UtcNow,
                LastLoginAtUtc = DateTime.UtcNow,
                UserId = userId
            });
        }
        else
        {
            existingIdentity.Email = userInfo.Email;
            existingIdentity.EmailVerified = userInfo.EmailVerified;
            existingIdentity.LastLoginAtUtc = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("External provider {Provider} linked for auth user {UserId}.", oidcProvider.ProviderName, userId);
    }

    public async Task UnlinkAsync(Guid userId, string provider, CancellationToken cancellationToken = default)
    {
        var normalizedProvider = NormalizeProviderName(provider);

        var authUser = await _dbContext.AuthUsers
            .Include(user => user.ExternalIdentities)
            .FirstOrDefaultAsync(user => user.Id == userId, cancellationToken)
            ?? throw new NotFoundException($"User with ID {userId} not found.");

        var identity = authUser.ExternalIdentities.FirstOrDefault(externalIdentity => externalIdentity.Provider == normalizedProvider)
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
        _logger.LogInformation("External provider {Provider} unlinked for auth user {UserId}.", normalizedProvider, userId);
    }

    private IOidcProviderStrategy GetRequiredProvider(string provider)
    {
        var normalizedProvider = NormalizeProviderName(provider);
        return _providerRegistry.GetProvider(normalizedProvider)
            ?? throw new NotFoundException($"Provider '{provider}' is not supported.");
    }

    private async Task<OidcStateEntry> TakeStateAsync(string provider, string stateValue, string operation, CancellationToken cancellationToken)
    {
        var state = await _stateStore.TakeAsync(stateValue, cancellationToken);
        if (state == null || !string.Equals(state.Provider, provider, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("External provider {Provider} {Operation} failed due to invalid or expired state.", provider, operation);
            throw new ValidationException("Invalid or expired external authentication state.");
        }

        return state;
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

    private static string NormalizeProviderName(string provider) => provider.Trim().ToLowerInvariant();
}
