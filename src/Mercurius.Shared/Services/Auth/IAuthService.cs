using Mercurius.LAN.API.DTOs.Auth;

namespace Mercurius.LAN.API.Services.Auth;

public interface IAuthService
{
    Task RegisterAsync(LoginRequest request);
    Task<AuthTokenResponse> LoginAsync(LoginRequest request);
    Task<AuthTokenResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task RevokeRefreshTokenAsync(RevokeTokenRequest request);
}
