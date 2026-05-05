using Mercurius.LAN.API.DTOs.Auth;
using Mercurius.LAN.API.Exceptions;

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
        if (string.IsNullOrWhiteSpace(request.Auth0Subject))
            throw new ValidationException("Auth0 subject is required.");

        ValidateProfileRequest(request.Username, request.Firstname, request.Lastname, request.Email);

        return _inner.CreateUserAsync(request);
    }

    public Task<GetUserDTO> CompleteProfileAsync(string auth0Subject, CompleteUserProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(auth0Subject))
            throw new ValidationException("Auth0 subject is required.");

        ValidateProfileRequest(request.Username, request.Firstname, request.Lastname, request.Email);
        return _inner.CompleteProfileAsync(auth0Subject, request);
    }

    public Task<CurrentUserProfileResponse> GetCurrentUserAsync(string auth0Subject)
    {
        if (string.IsNullOrWhiteSpace(auth0Subject))
            throw new ValidationException("Auth0 subject is required.");

        return _inner.GetCurrentUserAsync(auth0Subject);
    }

    public Task DeleteUserAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username) || !UserProfileValidationHelper.IsUsernameValid(username))
            throw new ValidationException("Invalid username.");

        var normalizedUsername = username.Normalize();
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

        ValidateProfileRequest(request.Username, request.Firstname, request.Lastname, request.Email);
        return _inner.UpdateUserAsync(id, request);
    }

    public Task<IEnumerable<GetUserDTO>> GetAllUsersAsync() => _inner.GetAllUsersAsync();
    public Task<GetUserDTO> GetUserByIdAsync(Guid id) => _inner.GetUserByIdAsync(id);

    private static void ValidateProfileRequest(string username, string firstname, string lastname, string email)
    {
        if (!UserProfileValidationHelper.IsUsernameValid(username))
            throw new ValidationException("Username must be 3-32 alphanumeric characters.");
        if (string.IsNullOrWhiteSpace(firstname) || string.IsNullOrWhiteSpace(lastname))
            throw new ValidationException("Firstname and lastname are required.");
        if (string.IsNullOrWhiteSpace(email))
            throw new ValidationException("Email is required.");
    }
}
