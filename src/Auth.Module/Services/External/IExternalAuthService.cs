using Auth.Module.Models;

namespace Auth.Module.Services.External;

public interface IExternalAuthService
{
    Task<OidcAuthStartResponse> StartAuthAsync(string provider, CancellationToken cancellationToken = default);
    Task<AuthTokenResponse> CompleteAuthAsync(string provider, OidcCallbackRequest request, CancellationToken cancellationToken = default);
    Task<OidcAuthStartResponse> StartLinkAsync(string provider, CancellationToken cancellationToken = default);
    Task CompleteLinkAsync(string provider, Guid userId, OidcCallbackRequest request, CancellationToken cancellationToken = default);
    Task UnlinkAsync(Guid userId, string provider, CancellationToken cancellationToken = default);
}
