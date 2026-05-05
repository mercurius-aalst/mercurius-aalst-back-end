using Auth.Module.Models;

namespace Auth.Module.Services.Token;

public interface ITokenService
{
    string GenerateJwtToken(AuthUser user);
    RefreshToken GenerateRefreshToken(Guid userId);
}
