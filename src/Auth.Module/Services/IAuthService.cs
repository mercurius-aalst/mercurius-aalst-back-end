using Auth.Module.Models;

namespace Auth.Module.Services;

public interface IAuthService
{
    Task RegisterAsync(LoginRequest request);
    Task<AuthTokenResponse> LoginAsync(LoginRequest request);
    Task<AuthTokenResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task RevokeRefreshTokenAsync(RevokeTokenRequest request);
}
