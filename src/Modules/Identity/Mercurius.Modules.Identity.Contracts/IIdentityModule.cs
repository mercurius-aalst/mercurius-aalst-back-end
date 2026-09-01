using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Identity.Contracts;

public interface IIdentityModule
{
    Task<UserProfileSummary?> GetUserProfileAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<UserProfileSummary?> GetUserProfileByAuth0IdAsync(
        string auth0UserId,
        CancellationToken cancellationToken = default);

    Task<PublicUserProfileSummary?> GetPublicProfileByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<UserId, UserProfileSummary>> GetUsersByIdsAsync(
        IReadOnlyCollection<UserId> userIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<UserId, string>> GetPublicUsernamesByIdsAsync(
        IReadOnlyCollection<UserId> userIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PublicUserSearchDocument>> GetPublicUserSearchDocumentsPageAsync(
        UserId? afterId,
        int pageSize,
        CancellationToken cancellationToken = default);
}
