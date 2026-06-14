using Mercurius.LAN.API.DTOs.Auth;
using Mercurius.LAN.API.Exceptions;
using Mercurius.LAN.API.Services.SearchServices;

namespace Mercurius.LAN.API.Services.UserServices;

/// <summary>
/// Decorator for IUserService that performs input validation before delegating to the actual business logic.
/// </summary>
public class UserValidationService : IUserService
{
    private readonly IUserService _inner;

    public UserValidationService(IUserService inner)
    {
        _inner = inner;
    }

    public Task<GetUserDTO> CreateUserAsync(CreateUserProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EffectiveAuth0UserId))
            throw new ValidationException("Auth0 user id is required.");

        ValidateProfileRequest(request.Username, request.Firstname, request.Lastname, request.DiscordId, request.SteamId, request.RiotId);

        return _inner.CreateUserAsync(request);
    }

    public Task<GetUserDTO> CreateCurrentUserAsync(string auth0UserId, CompleteUserProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(auth0UserId))
            throw new ValidationException("Auth0 user id is required.");

        ValidateProfileRequest(request.Username, request.Firstname, request.Lastname, request.DiscordId, request.SteamId, request.RiotId);
        return _inner.CreateCurrentUserAsync(auth0UserId, request);
    }

    public Task<GetUserDTO> CompleteProfileAsync(string auth0UserId, CompleteUserProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(auth0UserId))
            throw new ValidationException("Auth0 user id is required.");

        ValidateProfileRequest(request.Username, request.Firstname, request.Lastname, request.DiscordId, request.SteamId, request.RiotId);
        return _inner.CompleteProfileAsync(auth0UserId, request);
    }

    public Task<CurrentUserProfileResponse> GetCurrentUserAsync(string auth0UserId)
    {
        if (string.IsNullOrWhiteSpace(auth0UserId))
            throw new ValidationException("Auth0 user id is required.");

        return _inner.GetCurrentUserAsync(auth0UserId);
    }

    public Task<PublicUserProfileDTO> GetPublicUserProfileByUsernameAsync(string username)
    {
        var normalizedUsername = UserProfileValidationHelper.NormalizeUsername(username);
        if (!UserProfileValidationHelper.IsUsernameValid(normalizedUsername))
            throw new ValidationException("Username must be 3-32 alphanumeric characters.");

        return _inner.GetPublicUserProfileByUsernameAsync(normalizedUsername);
    }

    public Task<UserSearchResponseDTO> SearchUsersAsync(
        string? query,
        string? cursor,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = SearchRequest.NormalizeQuery(query);
        SearchRequest.ValidateQueryLength(normalizedQuery);
        SearchRequest.ValidatePageSize(pageSize);

        return _inner.SearchUsersAsync(normalizedQuery, cursor, pageSize, cancellationToken);
    }

    public Task<GetUserDTO> UpdateCurrentUserAsync(string auth0UserId, UpdateUserProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(auth0UserId))
            throw new ValidationException("Auth0 user id is required.");

        ValidateProfileRequest(request.Username, request.Firstname, request.Lastname, request.DiscordId, request.SteamId, request.RiotId);
        return _inner.UpdateCurrentUserAsync(auth0UserId, request);
    }

    public Task<UsernameAvailabilityResponse> CheckUsernameAvailabilityAsync(string auth0UserId, string username)
    {
        if (string.IsNullOrWhiteSpace(auth0UserId))
            throw new ValidationException("Auth0 user id is required.");

        return _inner.CheckUsernameAvailabilityAsync(auth0UserId, username);
    }

    public Task<UserActionResponse> ResendVerificationEmailAsync(string auth0UserId)
    {
        if (string.IsNullOrWhiteSpace(auth0UserId))
            throw new ValidationException("Auth0 user id is required.");

        return _inner.ResendVerificationEmailAsync(auth0UserId);
    }

    public Task<UserActionResponse> SendPasswordResetEmailAsync(string auth0UserId)
    {
        if (string.IsNullOrWhiteSpace(auth0UserId))
            throw new ValidationException("Auth0 user id is required.");

        return _inner.SendPasswordResetEmailAsync(auth0UserId);
    }

    public Task<UserActionResponse> AnonymizeCurrentUserAsync(string auth0UserId)
    {
        if (string.IsNullOrWhiteSpace(auth0UserId))
            throw new ValidationException("Auth0 user id is required.");

        return _inner.AnonymizeCurrentUserAsync(auth0UserId);
    }

    public Task DeleteUserAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username) || !UserProfileValidationHelper.IsUsernameValid(username))
            throw new ValidationException("Invalid username.");

        var normalizedUsername = UserProfileValidationHelper.NormalizeUsername(username);
        return _inner.DeleteUserAsync(normalizedUsername);
    }

    public Task DeleteUserByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ValidationException("Invalid user ID.");

        return _inner.DeleteUserByIdAsync(id);
    }

    public Task<GetUserDTO> UpdateUserAsync(Guid id, UpdateUserProfileRequest request)
    {
        if (id == Guid.Empty)
            throw new ValidationException("Invalid user ID.");

        ValidateProfileRequest(request.Username, request.Firstname, request.Lastname, request.DiscordId, request.SteamId, request.RiotId);
        return _inner.UpdateUserAsync(id, request);
    }

    public Task<IEnumerable<GetUserDTO>> GetAllUsersAsync() => _inner.GetAllUsersAsync();
    public Task<GetUserDTO> GetUserByIdAsync(Guid id) => _inner.GetUserByIdAsync(id);

    private static void ValidateProfileRequest(
        string username,
        string firstname,
        string lastname,
        string? discordId,
        string? steamId,
        string? riotId)
    {
        if (!UserProfileValidationHelper.IsUsernameValid(username))
            throw new ValidationException("Username must be 3-32 alphanumeric characters.");
        if (UserProfileValidationHelper.IsReservedUsername(username))
            throw new ValidationException("Username is reserved.");
        if (string.IsNullOrWhiteSpace(firstname) || string.IsNullOrWhiteSpace(lastname))
            throw new ValidationException("Firstname and lastname are required.");

        _ = UserProfileValidationHelper.NormalizeOptionalPlatformId(discordId, "Discord ID");
        _ = UserProfileValidationHelper.NormalizeOptionalPlatformId(steamId, "Steam ID");
        _ = UserProfileValidationHelper.NormalizeOptionalPlatformId(riotId, "Riot ID");
    }
}
