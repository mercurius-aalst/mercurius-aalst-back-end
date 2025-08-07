using MercuriusAPI.Models;
using MercuriusAPI.Models.Auth;

namespace MercuriusAPI.Services.Auth.Token
{
    public interface ITokenService
    {
        string GenerateJwtToken(User user);
        RefreshToken GenerateRefreshToken(int userId);
    }
}
