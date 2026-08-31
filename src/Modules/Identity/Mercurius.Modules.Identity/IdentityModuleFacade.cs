using Mercurius.Modules.Identity.Contracts;
using Mercurius.Modules.Identity.Domain;
using Mercurius.Modules.Identity.Infrastructure;
using Mercurius.Modules.Identity.Services;
using Mercurius.Modules.Shared;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.Modules.Identity;

internal sealed class IdentityModuleFacade : IIdentityModule
{
    private readonly IIdentityDbContext _dbContext;

    public IdentityModuleFacade(IIdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<UserProfileSummary?> GetUserProfileAsync(
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId.Value)
            .Select(user => ToUserProfileSummary(user))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<UserProfileSummary?> GetUserProfileByAuth0IdAsync(
        string auth0UserId,
        CancellationToken cancellationToken = default)
    {
        var normalizedAuth0UserId = auth0UserId.Trim();

        return _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Auth0UserId == normalizedAuth0UserId)
            .Select(user => ToUserProfileSummary(user))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<PublicUserProfileSummary?> GetPublicProfileByUsernameAsync(
        string username,
        CancellationToken cancellationToken = default)
    {
        var normalizedUsername = UserProfileValidationHelper.NormalizeUsername(username);

        return _dbContext.Users
            .AsNoTracking()
            .Where(user =>
                user.NormalizedUsername == normalizedUsername &&
                !user.IsDeleted &&
                !string.IsNullOrWhiteSpace(user.Username) &&
                !string.IsNullOrWhiteSpace(user.Firstname) &&
                !string.IsNullOrWhiteSpace(user.Lastname))
            .Select(user => new PublicUserProfileSummary(
                new UserId(user.Id),
                user.Username!,
                user.Firstname!,
                user.Lastname!,
                user.DiscordId,
                user.SteamId,
                user.RiotId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<UserId, UserProfileSummary>> GetUsersByIdsAsync(
        IReadOnlyCollection<UserId> userIds,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
            return new Dictionary<UserId, UserProfileSummary>();

        var ids = userIds.Select(userId => userId.Value).Distinct().ToArray();

        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(user => ids.Contains(user.Id))
            .Select(user => ToUserProfileSummary(user))
            .ToListAsync(cancellationToken);

        return users.ToDictionary(user => user.Id);
    }

    public async Task<IReadOnlyDictionary<UserId, string>> GetPublicUsernamesByIdsAsync(
        IReadOnlyCollection<UserId> userIds,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
            return new Dictionary<UserId, string>();

        var ids = userIds.Select(userId => userId.Value).Distinct().ToArray();
        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(user =>
                ids.Contains(user.Id) &&
                !user.IsDeleted &&
                !string.IsNullOrWhiteSpace(user.Username) &&
                !string.IsNullOrWhiteSpace(user.NormalizedUsername) &&
                !string.IsNullOrWhiteSpace(user.Firstname) &&
                !string.IsNullOrWhiteSpace(user.Lastname))
            .Select(user => new { user.Id, user.Username })
            .ToListAsync(cancellationToken);

        return users.ToDictionary(user => new UserId(user.Id), user => user.Username!);
    }

    public async Task<IReadOnlyList<PublicUserSearchDocument>> GetPublicUserSearchDocumentsPageAsync(
        UserId? afterId,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var afterValue = afterId?.Value;
        return await _dbContext.Users
            .AsNoTracking()
            .Where(user =>
                !user.IsDeleted &&
                !string.IsNullOrWhiteSpace(user.Username) &&
                !string.IsNullOrWhiteSpace(user.NormalizedUsername) &&
                !string.IsNullOrWhiteSpace(user.Firstname) &&
                !string.IsNullOrWhiteSpace(user.Lastname) &&
                (!afterValue.HasValue || user.Id > afterValue.Value))
            .OrderBy(user => user.Id)
            .Select(user => new PublicUserSearchDocument(
                new UserId(user.Id),
                user.Username!))
            .Take(Math.Clamp(pageSize, 1, 1000))
            .ToListAsync(cancellationToken);
    }

    private static UserProfileSummary ToUserProfileSummary(User user)
    {
        return new UserProfileSummary(
            new UserId(user.Id),
            user.Username,
            user.DisplayName,
            user.IsDeleted,
            user.DiscordId,
            user.SteamId,
            user.RiotId);
    }
}
