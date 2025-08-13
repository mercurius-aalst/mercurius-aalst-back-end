using MercuriusAPI.Services.Auth;
using MercuriusAPI.DTOs.Auth;
using MercuriusAPI.Exceptions;

namespace MercuriusAPI.Services.UserServices
{
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

        public Task DeleteUserAsync(string username)
        {
            if(string.IsNullOrWhiteSpace(username) || !ValidationHelper.IsUsernameValid(username))
                throw new ValidationException("Invalid username.");

            var normalizedUsername = username.Normalize();
            return _inner.DeleteUserAsync(normalizedUsername);
        }

        public Task AddRoleToUserAsync(string username, AddUserRoleRequest request)
        {
            if(request == null || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(request.RoleName))
                throw new ValidationException("Username and role name are required.");
            if(!ValidationHelper.IsUsernameValid(username))
                throw new ValidationException("Username must be 3-32 alphanumeric characters.");

            var normalizedUsername = username.Normalize();
            return _inner.AddRoleToUserAsync(normalizedUsername, request);
        }

        public Task ChangePasswordAsync(string username, string newPassword)
        {
            if(!ValidationHelper.IsPasswordStrong(newPassword))
                throw new ValidationException("Password must be at least 8 characters and include upper, lower, digit, and special character.");
            return _inner.ChangePasswordAsync(username, newPassword);
        }
    }
}
