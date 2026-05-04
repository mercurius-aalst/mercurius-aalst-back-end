using Mercurius.LAN.API.Models.Auth;

namespace Mercurius.LAN.API.Services.Auth.External;

public sealed class ExternalPrincipal
{
    public string Subject { get; init; } = string.Empty;
    public string? Email { get; init; }
    public bool EmailVerified { get; init; }
}

public interface IExternalTokenValidationService
{
    Task<ExternalPrincipal> ValidateAsync(ExternalAuthProvider provider, string token, CancellationToken cancellationToken = default);
}
