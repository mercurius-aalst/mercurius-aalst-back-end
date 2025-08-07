using MercuriusAPI.Models.Auth;

namespace MercuriusAPI.Services.Auth
{
    public interface ITokenService
    {
        string GenerateJwtToken(string username);
        RefreshToken GenerateRefreshToken(int userId);
    }
}