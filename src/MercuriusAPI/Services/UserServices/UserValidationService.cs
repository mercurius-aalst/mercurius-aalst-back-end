using Auth.Module.Services;
using Mercurius.LAN.API.DTOs.UserDTOs;
using Microsoft.Extensions.Configuration;
using Mercurius.Shared.Exceptions;

namespace Mercurius.LAN.API.Services.UserServices;

public class UserValidationService : IUserService
{
    private readonly IUserService _inner;

    public UserValidationService(IUserService inner)
    {
        _inner = inner;
    }

    public Task<GetUserDTO> CreateUserAsync(CreateUserProfileRequest request)
    {
        ValidateProfileRequest(request.Username, request.Firstname, request.Lastname, request.Email);

        if (!ValidationHelper.IsPasswordStrong(request.Password))
            throw new ValidationException("Password must be at least 8 characters and include upper, lower, digit, and special character.");

        return _inner.CreateUserAsync(request);
    }

    public Task DeleteUserAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username) || !ValidationHelper.IsUsernameValid(username))
            throw new ValidationException("Invalid username.");

        return _inner.DeleteUserAsync(username.Normalize());
    }

    public Task DeleteUserByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ValidationException("Invalid user ID.");

        return _inner.DeleteUserByIdAsync(id);
    }

    public Task AddRoleToUserAsync(string username, AddUserRoleRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(request.RoleName))
            throw new ValidationException("Username and role name are required.");

        if (!ValidationHelper.IsUsernameValid(username))
            throw new ValidationException("Username must be 3-32 alphanumeric characters.");

        return _inner.AddRoleToUserAsync(username.Normalize(), request);
    }

    public Task ChangePasswordAsync(string username, ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(username) || !ValidationHelper.IsUsernameValid(username))
            throw new ValidationException("Invalid username.");

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
            throw new ValidationException("Current password is required.");

        if (!ValidationHelper.IsPasswordStrong(request.NewPassword))
            throw new ValidationException("Password must be at least 8 characters and include upper, lower, digit, and special character.");

        if (ValidationHelper.IsPasswordSame(request.CurrentPassword, request.NewPassword))
            throw new ValidationException("New password must be different from the old password.");

        return _inner.ChangePasswordAsync(username.Normalize(), request);
    }

    public Task<IEnumerable<GetUserDTO>> GetAllUsersAsync() => _inner.GetAllUsersAsync();

    public Task<GetUserDTO> GetUserByIdAsync(Guid id) => _inner.GetUserByIdAsync(id);

    public Task<GetUserDTO> UpdateUserAsync(Guid id, UpdateUserProfileRequest request)
    {
        if (id == Guid.Empty)
            throw new ValidationException("Invalid user ID.");

        ValidateProfileRequest(request.Username, request.Firstname, request.Lastname, request.Email);
        return _inner.UpdateUserAsync(id, request);
    }

    public Task DeleteRoleFromUserAsync(string username, string roleName)
    {
        if (string.IsNullOrWhiteSpace(username) || !ValidationHelper.IsUsernameValid(username))
            throw new ValidationException("Invalid username.");

        if (string.IsNullOrWhiteSpace(roleName))
            throw new ValidationException("Role name is required.");

        return _inner.DeleteRoleFromUserAsync(username.Normalize(), roleName);
    }

    public Task SeedInitialUserAsync(IConfiguration configuration) => _inner.SeedInitialUserAsync(configuration);

    private static void ValidateProfileRequest(string username, string firstname, string lastname, string email)
    {
        if (!ValidationHelper.IsUsernameValid(username))
            throw new ValidationException("Username must be 3-32 alphanumeric characters.");
        if (string.IsNullOrWhiteSpace(firstname) || string.IsNullOrWhiteSpace(lastname))
            throw new ValidationException("Firstname and lastname are required.");
        if (string.IsNullOrWhiteSpace(email))
            throw new ValidationException("Email is required.");
    }
}
