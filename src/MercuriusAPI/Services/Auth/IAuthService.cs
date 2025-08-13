using System.Threading.Tasks;
using MercuriusAPI.DTOs.Auth;

namespace MercuriusAPI.Services.Auth
{
    public interface IAuthService
    {
        Task RegisterAsync(LoginRequest request);
        Task<AuthTokenResponse> LoginAsync(LoginRequest request);
        Task<AuthTokenResponse> RefreshTokenAsync(RefreshTokenRequest request);
        Task RevokeRefreshTokenAsync(RevokeTokenRequest request);
    }
}
