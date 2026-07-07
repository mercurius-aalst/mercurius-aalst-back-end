using Mercurius.Modules.Identity.Contracts;
using Mercurius.Modules.Identity.DTOs;
using Mercurius.Modules.Identity.Infrastructure;
using Mercurius.Modules.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Platform.Eventing;

namespace Mercurius.Modules.Identity.Services;

public sealed class UserIntegrationEventPublishingService : IUserService
{
    private readonly IUserService _inner;
    private readonly IIdentityDbContext _dbContext;
    private readonly IModuleEventPublisher _moduleEventPublisher;

    public UserIntegrationEventPublishingService(
        IUserService inner,
        IIdentityDbContext dbContext,
        IModuleEventPublisher moduleEventPublisher)
    {
        _inner = inner;
        _dbContext = dbContext;
        _moduleEventPublisher = moduleEventPublisher;
    }

    public async Task<GetUserDTO> CreateUserAsync(CreateUserProfileRequest request)
    {
        return await ExecuteProfileMutationAsync(
            () => _inner.CreateUserAsync(request),
            before: null);
    }

    public async Task<GetUserDTO> CreateCurrentUserAsync(string auth0UserId, CompleteUserProfileRequest request)
    {
        return await ExecuteProfileMutationAsync(
            () => _inner.CreateCurrentUserAsync(auth0UserId, request),
            before: null);
    }

    public async Task<GetUserDTO> CompleteProfileAsync(string auth0UserId, CompleteUserProfileRequest request)
    {
        await using var transaction = await BeginTransactionIfSupportedAsync();
        var before = await GetUserByAuth0IdAsync(auth0UserId);
        var user = await _inner.CompleteProfileAsync(auth0UserId, request);

        await PublishEventsAsync(BuildProfileEvents(user, before));
        await CommitIfStartedAsync(transaction);
        return user;
    }

    public Task<CurrentUserProfileResponse> GetCurrentUserAsync(string auth0UserId)
    {
        return _inner.GetCurrentUserAsync(auth0UserId);
    }

    public Task<PublicUserProfileDTO> GetPublicUserProfileByUsernameAsync(string username)
    {
        return _inner.GetPublicUserProfileByUsernameAsync(username);
    }

    public Task<UserSearchResponseDTO> SearchUsersAsync(
        string? query,
        string? cursor,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        return _inner.SearchUsersAsync(query, cursor, pageSize, cancellationToken);
    }

    public async Task<GetUserDTO> UpdateCurrentUserAsync(string auth0UserId, UpdateUserProfileRequest request)
    {
        await using var transaction = await BeginTransactionIfSupportedAsync();
        var before = await GetUserByAuth0IdAsync(auth0UserId);
        var user = await _inner.UpdateCurrentUserAsync(auth0UserId, request);

        await PublishEventsAsync(BuildProfileEvents(user, before));
        await CommitIfStartedAsync(transaction);
        return user;
    }

    public Task<UsernameAvailabilityResponse> CheckUsernameAvailabilityAsync(string auth0UserId, string username)
    {
        return _inner.CheckUsernameAvailabilityAsync(auth0UserId, username);
    }

    public Task<UserActionResponse> ResendVerificationEmailAsync(string auth0UserId)
    {
        return _inner.ResendVerificationEmailAsync(auth0UserId);
    }

    public Task<UserActionResponse> SendPasswordResetEmailAsync(string auth0UserId)
    {
        return _inner.SendPasswordResetEmailAsync(auth0UserId);
    }

    public async Task<UserActionResponse> AnonymizeCurrentUserAsync(string auth0UserId)
    {
        await using var transaction = await BeginTransactionIfSupportedAsync();
        var before = await GetUserByAuth0IdAsync(auth0UserId);
        var response = await _inner.AnonymizeCurrentUserAsync(auth0UserId);
        var after = before is null ? null : await GetUserEventStateByIdAsync(before.Id);

        await PublishEventsAsync(BuildDeletionEvents(before, after));
        await CommitIfStartedAsync(transaction);
        return response;
    }

    public async Task DeleteUserAsync(string username)
    {
        await using var transaction = await BeginTransactionIfSupportedAsync();
        var before = await GetUserByNormalizedUsernameAsync(username);
        await _inner.DeleteUserAsync(username);
        var after = before is null ? null : await GetUserEventStateByIdAsync(before.Id);

        await PublishEventsAsync(BuildDeletionEvents(before, after));
        await CommitIfStartedAsync(transaction);
    }

    public async Task DeleteUserByIdAsync(Guid id)
    {
        await using var transaction = await BeginTransactionIfSupportedAsync();
        var before = await GetUserEventStateByIdAsync(id);
        await _inner.DeleteUserByIdAsync(id);
        var after = before is null ? null : await GetUserEventStateByIdAsync(before.Id);

        await PublishEventsAsync(BuildDeletionEvents(before, after));
        await CommitIfStartedAsync(transaction);
    }

    public Task<IEnumerable<GetUserDTO>> GetAllUsersAsync()
    {
        return _inner.GetAllUsersAsync();
    }

    public Task<GetUserDTO> GetUserByIdAsync(Guid id)
    {
        return _inner.GetUserByIdAsync(id);
    }

    public async Task<GetUserDTO> UpdateUserAsync(Guid id, UpdateUserProfileRequest request)
    {
        await using var transaction = await BeginTransactionIfSupportedAsync();
        var before = await GetUserEventStateByIdAsync(id);
        var user = await _inner.UpdateUserAsync(id, request);

        await PublishEventsAsync(BuildProfileEvents(user, before));
        await CommitIfStartedAsync(transaction);
        return user;
    }

    private async Task<GetUserDTO> ExecuteProfileMutationAsync(
        Func<Task<GetUserDTO>> mutation,
        UserEventState? before)
    {
        await using var transaction = await BeginTransactionIfSupportedAsync();
        var user = await mutation();

        await PublishEventsAsync(BuildProfileEvents(user, before));
        await CommitIfStartedAsync(transaction);
        return user;
    }

    private async Task PublishEventsAsync(IReadOnlyCollection<object> events)
    {
        if (events.Count == 0)
            return;

        foreach (var integrationEvent in events)
            _moduleEventPublisher.Publish(integrationEvent);

        await _dbContext.SaveChangesAsync();
    }

    private static IReadOnlyCollection<object> BuildProfileEvents(GetUserDTO user, UserEventState? before)
    {
        var events = new List<object>
        {
            new UserProfileChangedIntegrationEvent(
                new UserId(user.Id),
                user.Username,
                user.DisplayName,
                user.IsDeleted,
                user.UpdatedAtUtc)
        };

        if (UsernameChanged(before, user) && !string.IsNullOrWhiteSpace(user.Username))
        {
            events.Add(new UsernameChangedIntegrationEvent(
                new UserId(user.Id),
                user.Username,
                user.UpdatedAtUtc));
        }

        return events;
    }

    private static IReadOnlyCollection<object> BuildDeletionEvents(UserEventState? before, UserEventState? after)
    {
        if (before is null || before.IsDeleted || after is null || !after.IsDeleted || after.DeletedAtUtc is null)
            return [];

        var events = new List<object>
        {
            new UserAnonymizedIntegrationEvent(new UserId(after.Id), after.DeletedAtUtc.Value),
            new UserDeletedIntegrationEvent(new UserId(after.Id), after.DeletedAtUtc.Value),
            new UserProfileChangedIntegrationEvent(
                new UserId(after.Id),
                after.Username,
                after.DisplayName,
                after.IsDeleted,
                after.UpdatedAtUtc)
        };

        if (!string.IsNullOrWhiteSpace(after.Username))
        {
            events.Add(new UsernameChangedIntegrationEvent(
                new UserId(after.Id),
                after.Username,
                after.UpdatedAtUtc));
        }

        return events;
    }

    private static bool UsernameChanged(UserEventState? before, GetUserDTO after)
    {
        return before is null ||
            !string.Equals(before.Username, after.Username, StringComparison.Ordinal);
    }

    private Task<UserEventState?> GetUserByAuth0IdAsync(string auth0UserId)
    {
        var normalizedAuth0UserId = auth0UserId.Trim();
        return _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Auth0UserId == normalizedAuth0UserId)
            .Select(user => new UserEventState(
                user.Id,
                user.Username,
                user.DisplayName,
                user.IsDeleted,
                user.DeletedAtUtc,
                user.UpdatedAtUtc))
            .FirstOrDefaultAsync();
    }

    private Task<UserEventState?> GetUserByNormalizedUsernameAsync(string username)
    {
        var normalizedUsername = UserProfileValidationHelper.NormalizeUsername(username);
        return _dbContext.Users
            .AsNoTracking()
            .Where(user => user.NormalizedUsername == normalizedUsername)
            .Select(user => new UserEventState(
                user.Id,
                user.Username,
                user.DisplayName,
                user.IsDeleted,
                user.DeletedAtUtc,
                user.UpdatedAtUtc))
            .FirstOrDefaultAsync();
    }

    private Task<UserEventState?> GetUserEventStateByIdAsync(Guid id)
    {
        return _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == id)
            .Select(user => new UserEventState(
                user.Id,
                user.Username,
                user.DisplayName,
                user.IsDeleted,
                user.DeletedAtUtc,
                user.UpdatedAtUtc))
            .FirstOrDefaultAsync();
    }

    private async Task<IDbContextTransaction?> BeginTransactionIfSupportedAsync()
    {
        if (_dbContext is not DbContext dbContext ||
            dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory" ||
            dbContext.Database.CurrentTransaction is not null)
        {
            return null;
        }

        return await dbContext.Database.BeginTransactionAsync();
    }

    private static async Task CommitIfStartedAsync(IDbContextTransaction? transaction)
    {
        if (transaction is not null)
            await transaction.CommitAsync();
    }

    private sealed record UserEventState(
        Guid Id,
        string? Username,
        string DisplayName,
        bool IsDeleted,
        DateTime? DeletedAtUtc,
        DateTime UpdatedAtUtc);
}
