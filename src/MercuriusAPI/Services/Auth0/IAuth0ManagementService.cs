using Mercurius.LAN.API.DTOs.Auth;

namespace Mercurius.LAN.API.Services.Auth0;

public interface IAuth0ManagementService
{
    Task<Auth0ProfileSnapshot> GetUserProfileAsync(string auth0UserId, CancellationToken cancellationToken = default);
    Task SendVerificationEmailAsync(string auth0UserId, CancellationToken cancellationToken = default);
    Task SendPasswordResetEmailAsync(string email, CancellationToken cancellationToken = default);
}
