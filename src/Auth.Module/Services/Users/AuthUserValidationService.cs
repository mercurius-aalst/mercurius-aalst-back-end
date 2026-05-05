using Auth.Module.Exceptions;
using Auth.Module.Models;
using Microsoft.Extensions.Configuration;

namespace Auth.Module.Services.Users;

public class AuthUserValidationService : IAuthUserService
{
    private readonly IAuthUserService _inner;

    public AuthUserValidationService(IAuthUserService inner)
    {
        _inner = inner;
    }

    public Task<Guid> CreateAuthUserAsync(string username, string password)
    {
        if (!ValidationHelper.IsUsernameValid(username))
            throw new ValidationException("Username must be 3-32 alphanumeric characters.");

        if (!ValidationHelper.IsPasswordStrong(password))
            throw new ValidationException("Password must be at least 8 characters and include upper, lower, digit, and special character.");

        return _inner.CreateAuthUserAsync(username, password);
    }

    public Task<Guid> CreateExternalAuthUserAsync(string username)
    {
        if (!ValidationHelper.IsUsernameValid(username))
            throw new ValidationException("Username must be 3-32 alphanumeric characters.");

        return _inner.CreateExternalAuthUserAsync(username);
    }

    public Task<GetAuthUserDTO> GetAuthUserByIdAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ValidationException("Invalid user ID.");

        return _inner.GetAuthUserByIdAsync(id);
    }

    public Task UpdateUsernameAsync(Guid id, string username)
    {
        if (id == Guid.Empty)
            throw new ValidationException("Invalid user ID.");

        if (!ValidationHelper.IsUsernameValid(username))
            throw new ValidationException("Username must be 3-32 alphanumeric characters.");

        return _inner.UpdateUsernameAsync(id, username);
    }

    public Task DeleteAuthUserAsync(Guid id)
    {
        if (id == Guid.Empty)
            throw new ValidationException("Invalid user ID.");

        return _inner.DeleteAuthUserAsync(id);
    }

    public Task AddRoleToUserAsync(Guid id, string roleName)
    {
        if (id == Guid.Empty)
            throw new ValidationException("Invalid user ID.");

        if (string.IsNullOrWhiteSpace(roleName))
            throw new ValidationException("Role name is required.");

        return _inner.AddRoleToUserAsync(id, roleName);
    }

    public Task RemoveRoleFromUserAsync(Guid id, string roleName)
    {
        if (id == Guid.Empty)
            throw new ValidationException("Invalid user ID.");

        if (string.IsNullOrWhiteSpace(roleName))
            throw new ValidationException("Role name is required.");

        return _inner.RemoveRoleFromUserAsync(id, roleName);
    }

    public Task ChangePasswordAsync(Guid id, string currentPassword, string newPassword)
    {
        if (id == Guid.Empty)
            throw new ValidationException("Invalid user ID.");

        if (string.IsNullOrWhiteSpace(currentPassword))
            throw new ValidationException("Current password is required.");

        if (!ValidationHelper.IsPasswordStrong(newPassword))
            throw new ValidationException("Password must be at least 8 characters and include upper, lower, digit, and special character.");

        if (ValidationHelper.IsPasswordSame(currentPassword, newPassword))
            throw new ValidationException("New password must be different from the old password.");

        return _inner.ChangePasswordAsync(id, currentPassword, newPassword);
    }

    public Task SeedInitialUserAsync(IConfiguration configuration) => _inner.SeedInitialUserAsync(configuration);
}
