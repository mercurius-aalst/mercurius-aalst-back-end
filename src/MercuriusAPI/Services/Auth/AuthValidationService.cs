using MercuriusAPI.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MercuriusAPI.Services.Auth
{
    /// <summary>
    /// Decorator for IAuthService that performs input validation before delegating to the actual business logic.
    /// </summary>
    public class AuthValidationService : IAuthService
    {
        private readonly IAuthService _inner;

        public AuthValidationService(IAuthService inner)
        {
            _inner = inner;
        }

        public async Task<IActionResult> RegisterAsync(LoginRequest request)
        {
            if (request == null)
                return new BadRequestObjectResult("Request body is required.");
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return new BadRequestObjectResult("Username and password are required.");
            if (!ValidationHelper.IsUsernameValid(request.Username))
                return new BadRequestObjectResult("Username must be 3-32 alphanumeric characters.");
            if (!ValidationHelper.IsPasswordStrong(request.Password))
                return new BadRequestObjectResult("Password must be at least 8 characters and include upper, lower, digit, and special character.");
            return await _inner.RegisterAsync(request);
        }

        public async Task<IActionResult> LoginAsync(LoginRequest request)
        {
            if (request == null)
                return new BadRequestObjectResult("Request body is required.");
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return new BadRequestObjectResult("Username and password are required.");
            if (!ValidationHelper.IsUsernameValid(request.Username))
                return new BadRequestObjectResult("Username must be 3-32 alphanumeric characters.");
            return await _inner.LoginAsync(request);
        }

        public async Task<IActionResult> RefreshTokenAsync(RefreshTokenRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
                return new BadRequestObjectResult("Refresh token is required.");
            return await _inner.RefreshTokenAsync(request);
        }

        public async Task<IActionResult> RevokeRefreshTokenAsync(RevokeTokenRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
                return new BadRequestObjectResult("Refresh token is required.");
            return await _inner.RevokeRefreshTokenAsync(request);
        }

        public async Task<IActionResult> DeleteUserAsync(string username)
        {
            if (!ValidationHelper.IsUsernameValid(username))
                return new BadRequestObjectResult("Invalid username.");
            return await _inner.DeleteUserAsync(username);
        }
    }
}
