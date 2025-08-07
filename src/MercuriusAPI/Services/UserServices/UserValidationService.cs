using MercuriusAPI.DTOs.Auth;
using MercuriusAPI.Exceptions;
using MercuriusAPI.Services.Auth;

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
            if (string.IsNullOrWhiteSpace(username) || !ValidationHelper.IsUsernameValid(username))
                throw new ValidationException("Invalid username.");
            return _inner.DeleteUserAsync(username);
        }

        public Task AddRoleToUserAsync(AddUserRoleRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.RoleName))
                throw new ValidationException("Username and role name are required.");
            if (!ValidationHelper.IsUsernameValid(request.Username))
                throw new ValidationException("Username must be 3-32 alphanumeric characters.");
            return _inner.AddRoleToUserAsync(request);
        }
    }
}
