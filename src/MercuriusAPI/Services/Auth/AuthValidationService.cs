using MercuriusAPI.DTOs.Auth;
using MercuriusAPI.Exceptions;

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

        public Task RegisterAsync(LoginRequest request)
        {
            if (request == null)
                throw new ValidationException("Request body is required.");
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                throw new ValidationException("Username and password are required.");
            if (!ValidationHelper.IsUsernameValid(request.Username))
                throw new ValidationException("Username must be 3-32 alphanumeric characters.");
            if (!ValidationHelper.IsPasswordStrong(request.Password))
                throw new ValidationException("Password must be at least 8 characters and include upper, lower, digit, and special character.");
            return _inner.RegisterAsync(request);
        }

        public Task<AuthTokenResponse> LoginAsync(LoginRequest request)
        {
            if (request == null)
                throw new ValidationException("Request body is required.");
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                throw new ValidationException("Username and password are required.");
            if (!ValidationHelper.IsUsernameValid(request.Username))
                throw new ValidationException("Username must be 3-32 alphanumeric characters.");
            return _inner.LoginAsync(request);
        }

        public Task<AuthTokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
                throw new ValidationException("Refresh token is required.");
            return _inner.RefreshTokenAsync(request);
        }

        public Task RevokeRefreshTokenAsync(RevokeTokenRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
                throw new ValidationException("Refresh token is required.");
            return _inner.RevokeRefreshTokenAsync(request);
        }
    }
}
