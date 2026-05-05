using Mercurius.Shared.Models.Auth;

namespace Mercurius.LAN.API.Services.Auth.Token;

public interface ITokenService
{
    string GenerateJwtToken(User user);
    RefreshToken GenerateRefreshToken(Guid userId);
}
