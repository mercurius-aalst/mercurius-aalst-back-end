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
        private readonly IAuthService _authService;

        public UserValidationService(IUserService inner, IAuthService authService)
        {
            _inner = inner;
            _authService = authService;
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

        public async Task ChangePasswordAsync(string username, ChangePasswordRequest request)
        {
            await _authService.LoginAsync(new LoginRequest { Password = request.CurrentPassword, Username = username }); 
            if(!ValidationHelper.IsPasswordStrong(request.NewPassword))
                throw new ValidationException("Password must be at least 8 characters and include upper, lower, digit, and special character.");
            if(ValidationHelper.IsPasswordSame(request.CurrentPassword, request.NewPassword))
                throw new ValidationException("New password must be different from the old password.");
            await _inner.ChangePasswordAsync(username, request);
        }

        public Task<IEnumerable<GetUserDTO>> GetAllUsersAsync() => _inner.GetAllUsersAsync();
        public Task<GetUserDTO> GetUserByIdAsync(int id) => _inner.GetUserByIdAsync(id);
        public Task DeleteRoleFromUserAsync(string username, string roleName) => _inner.DeleteRoleFromUserAsync(username, roleName);
        public Task SeedInitialUserAsync(IConfiguration configuration) => _inner.SeedInitialUserAsync(configuration);
    }
}
