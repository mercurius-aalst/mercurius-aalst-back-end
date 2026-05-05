using Mercurius.Shared.Models.Auth;

namespace Auth.Module.Services.Token;

public interface ITokenService
{
    string GenerateJwtToken(User user);
    RefreshToken GenerateRefreshToken(Guid userId);
}
