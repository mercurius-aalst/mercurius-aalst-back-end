using MercuriusAPI.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MercuriusAPI.Services.Auth
{
    public interface IAuthService
    {
        Task<IActionResult> RegisterAsync(LoginRequest request);
        Task<IActionResult> LoginAsync(LoginRequest request);
        Task<IActionResult> RefreshTokenAsync(RefreshTokenRequest request);
        Task<IActionResult> RevokeRefreshTokenAsync(RevokeTokenRequest request);
        Task<IActionResult> DeleteUserAsync(string username);
    }
}
