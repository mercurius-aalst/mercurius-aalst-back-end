using Auth.Module.Models;

namespace Auth.Module.Services.External;

public interface IExternalAuthService
{
    Task<GoogleAuthStartResponse> StartGoogleAuthAsync(CancellationToken cancellationToken = default);
    Task<AuthTokenResponse> CompleteGoogleAuthAsync(GoogleAuthCallbackRequest request, CancellationToken cancellationToken = default);
    Task<GoogleAuthStartResponse> StartGoogleLinkAsync(CancellationToken cancellationToken = default);
    Task CompleteGoogleLinkAsync(Guid userId, GoogleAuthCallbackRequest request, CancellationToken cancellationToken = default);
    Task UnlinkExternalIdentityAsync(Guid userId, string provider, CancellationToken cancellationToken = default);
}
