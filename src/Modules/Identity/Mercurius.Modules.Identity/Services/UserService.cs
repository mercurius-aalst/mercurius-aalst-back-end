using Mercurius.Modules.Identity.Domain;
using Mercurius.Modules.Identity.DTOs;
using Mercurius.Modules.Identity.Infrastructure;
using Mercurius.Modules.Shared.Exceptions;
using Mercurius.Modules.Identity.Services.Auth0;
using Mercurius.Modules.Shared.Search;
using Microsoft.EntityFrameworkCore;
using Platform.Eventing;
using UserAnonymizedIntegrationEvent = Mercurius.Modules.Identity.Contracts.UserAnonymizedIntegrationEvent;
using UserDeletedIntegrationEvent = Mercurius.Modules.Identity.Contracts.UserDeletedIntegrationEvent;
using UserId = Mercurius.Modules.Shared.UserId;
using UserProfileChangedIntegrationEvent = Mercurius.Modules.Identity.Contracts.UserProfileChangedIntegrationEvent;
using UsernameChangedIntegrationEvent = Mercurius.Modules.Identity.Contracts.UsernameChangedIntegrationEvent;

namespace Mercurius.Modules.Identity.Services;

public class UserService : IUserService
{
    private const string GenericVerificationMessage = "If verification is available for this account, a verification email has been sent.";
    private const string GenericPasswordResetMessage = "If password reset is available for this account, a password reset email has been sent.";
    private readonly IIdentityDbContext _dbContext;
    private readonly IAuth0ManagementService _auth0ManagementService;
    private readonly IModuleEventPublisher? _moduleEventPublisher;

    public UserService(
        IIdentityDbContext dbContext,
        IAuth0ManagementService auth0ManagementService,
        IModuleEventPublisher? moduleEventPublisher = null)
    {
        _dbContext = dbContext;
        _auth0ManagementService = auth0ManagementService;
        _moduleEventPublisher = moduleEventPublisher;
    }

    public async Task<IEnumerable<GetUserDTO>> GetAllUsersAsync()
    {
        return await _dbContext.Users.Select(u => new GetUserDTO(u)).ToListAsync();
    }

    public async Task<GetUserDTO> CreateUserAsync(CreateUserProfileRequest request)
    {
        var auth0UserId = NormalizeAuth0UserId(request.EffectiveAuth0UserId);
        var user = await CreateIncompleteUserAsync(
            auth0UserId,
            new Auth0ProfileSnapshot(request.Email, request.EmailVerified, false));

        var usernameChanged = ApplyProfileUpdate(user, request.Username, request.Firstname, request.Lastname, request.DiscordId, request.SteamId, request.RiotId);
        PublishProfileChanged(user, usernameChanged);

        await SaveProfileChangesAsync();
        return new GetUserDTO(user);
    }

    public async Task<GetUserDTO> CreateCurrentUserAsync(string auth0UserId, CompleteUserProfileRequest request)
    {
        var normalizedAuth0UserId = NormalizeAuth0UserId(auth0UserId);
        if (await _dbContext.Users.AnyAsync(u => u.Auth0UserId == normalizedAuth0UserId))
            throw new ValidationException("Current user profile already exists.");

        var auth0Profile = await _auth0ManagementService.GetUserProfileAsync(normalizedAuth0UserId);
        var user = await CreateIncompleteUserAsync(normalizedAuth0UserId, auth0Profile);

        var usernameChanged = ApplyProfileUpdate(user, request.Username, request.Firstname, request.Lastname, request.DiscordId, request.SteamId, request.RiotId);
        PublishProfileChanged(user, usernameChanged);

        await SaveProfileChangesAsync();
        return new GetUserDTO(user);
    }

    public async Task<GetUserDTO> CompleteProfileAsync(string auth0UserId, CompleteUserProfileRequest request)
    {
        var user = await GetRequiredCurrentUserAsync(auth0UserId);
        EnsureActive(user);

        var usernameChanged = ApplyProfileUpdate(user, request.Username, request.Firstname, request.Lastname, request.DiscordId, request.SteamId, request.RiotId);
        PublishProfileChanged(user, usernameChanged);

        await SaveProfileChangesAsync();
        return new GetUserDTO(user);
    }

    public async Task<CurrentUserProfileResponse> GetCurrentUserAsync(string auth0UserId)
    {
        var user = await GetRequiredCurrentUserAsync(auth0UserId);
        EnsureActive(user);

        return new CurrentUserProfileResponse(user.IsComplete, new GetUserDTO(user));
    }

    public async Task<PublicUserProfileDTO> GetPublicUserProfileByUsernameAsync(string username)
    {
        var normalizedUsername = UserProfileValidationHelper.NormalizeUsername(username);
        var trimmedUsername = username?.Trim() ?? string.Empty;

        var profile = await _dbContext.Users
            .AsNoTracking()
            .Where(u =>
                u.NormalizedUsername == normalizedUsername &&
                !u.IsDeleted &&
                u.Username != null &&
                u.NormalizedUsername != null &&
                u.Firstname != null &&
                u.Lastname != null &&
                u.Username != string.Empty &&
                u.NormalizedUsername != string.Empty &&
                u.Firstname != string.Empty &&
                u.Lastname != string.Empty)
            .Select(u => new PublicUserProfileDTO
            {
                Username = u.Username!,
                Firstname = u.Firstname!,
                Lastname = u.Lastname!,
                DiscordId = u.DiscordId,
                SteamId = u.SteamId,
                RiotId = u.RiotId
            })
            .FirstOrDefaultAsync();

        if (profile == null)
            throw new NotFoundException($"User '{trimmedUsername}' not found.");

        return profile;
    }

    public async Task<UserSearchResponseDTO> SearchUsersAsync(
        string? query,
        string? cursor,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = SearchRequest.NormalizeQuery(query);
        SearchRequest.ValidateQueryLength(normalizedQuery);

        if (normalizedQuery.Length < SearchRequestLimits.MinimumQueryLength)
            return new UserSearchResponseDTO { Results = [], HasMore = false };

        var boundedPageSize = SearchRequest.BoundPageSize(pageSize);
        var decodedCursor = DecodeUserSearchCursor(cursor, normalizedQuery);
        var users = await BuildPagedUserSearchQuery(normalizedQuery, decodedCursor, boundedPageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = users.Count > boundedPageSize;
        if (hasMore)
            users.RemoveAt(users.Count - 1);

        return new UserSearchResponseDTO
        {
            Results = users.Select(ToUserSearchResult).ToList(),
            NextCursor = hasMore ? BuildUserSearchCursor(normalizedQuery, users[^1]) : null,
            HasMore = hasMore
        };
    }

    private IQueryable<UserSearchCandidate> BuildPagedUserSearchQuery(string normalizedQuery, UserSearchCursor? cursor, int limit)
    {
        return ApplyUserSearchCursor(BuildUserSearchQuery(normalizedQuery), cursor)
            .OrderBy(user => user.RelevanceRank)
            .ThenBy(user => user.NormalizedUsername)
            .ThenBy(user => user.Id)
            .Take(limit);
    }

    private IQueryable<UserSearchCandidate> BuildUserSearchQuery(string normalizedQuery)
    {
        var escapedQuery = SearchRequest.EscapeLikePattern(normalizedQuery);
        var containsPattern = $"%{escapedQuery}%";
        var prefixPattern = $"{escapedQuery}%";

        return _dbContext.Users
            .AsNoTracking()
            .Where(user =>
                !user.IsDeleted &&
                !string.IsNullOrEmpty(user.Username) &&
                !string.IsNullOrEmpty(user.NormalizedUsername) &&
                EF.Functions.Like(user.NormalizedUsername, containsPattern, "\\"))
            .Select(user => new UserSearchCandidate
            {
                Id = user.Id,
                RelevanceRank = user.NormalizedUsername == normalizedQuery
                    ? 0
                    : EF.Functions.Like(user.NormalizedUsername, prefixPattern, "\\") ? 1 : 2,
                NormalizedUsername = user.NormalizedUsername!,
                Username = user.Username!
            });
    }

    private static IQueryable<UserSearchCandidate> ApplyUserSearchCursor(IQueryable<UserSearchCandidate> candidates, UserSearchCursor? cursor)
    {
        if (cursor is null)
            return candidates;

        return candidates.Where(candidate =>
            (candidate.RelevanceRank > cursor.RelevanceRank) ||
            (candidate.RelevanceRank == cursor.RelevanceRank &&
             string.Compare(candidate.NormalizedUsername, cursor.NormalizedUsername) > 0) ||
            (candidate.RelevanceRank == cursor.RelevanceRank &&
             candidate.NormalizedUsername == cursor.NormalizedUsername &&
             candidate.Id.CompareTo(cursor.StableId) > 0));
    }

    private static UserSearchResultDTO ToUserSearchResult(UserSearchCandidate candidate)
    {
        return new UserSearchResultDTO
        {
            Id = candidate.Id,
            Type = "user",
            Username = candidate.Username,
            DisplayLabel = candidate.Username,
            SupportingText = "User"
        };
    }

    public async Task<GetUserDTO> UpdateCurrentUserAsync(string auth0UserId, UpdateUserProfileRequest request)
    {
        var user = await GetRequiredCurrentUserAsync(auth0UserId);
        EnsureActive(user);

        var usernameChanged = ApplyProfileUpdate(user, request.Username, request.Firstname, request.Lastname, request.DiscordId, request.SteamId, request.RiotId);
        PublishProfileChanged(user, usernameChanged);

        await SaveProfileChangesAsync();
        return new GetUserDTO(user);
    }

    public async Task<UsernameAvailabilityResponse> CheckUsernameAvailabilityAsync(string auth0UserId, string username)
    {
        var normalizedAuth0UserId = NormalizeAuth0UserId(auth0UserId);
        var trimmedUsername = username?.Trim() ?? string.Empty;
        if (!UserProfileValidationHelper.IsUsernameValid(trimmedUsername))
        {
            return new UsernameAvailabilityResponse
            {
                Username = trimmedUsername,
                IsAvailable = false,
                Reason = "Username must be 3-32 alphanumeric characters."
            };
        }

        var normalizedUsername = UserProfileValidationHelper.NormalizeUsername(trimmedUsername);
        if (UserProfileValidationHelper.IsReservedUsername(trimmedUsername))
        {
            return new UsernameAvailabilityResponse
            {
                Username = trimmedUsername,
                NormalizedUsername = normalizedUsername,
                IsAvailable = false,
                Reason = "Username is reserved."
            };
        }

        var currentUserId = await _dbContext.Users
            .Where(u => u.Auth0UserId == normalizedAuth0UserId)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync();

        var exists = await _dbContext.Users.AnyAsync(u =>
            u.NormalizedUsername == normalizedUsername &&
            !u.IsDeleted &&
            (!currentUserId.HasValue || u.Id != currentUserId.Value));

        return new UsernameAvailabilityResponse
        {
            Username = trimmedUsername,
            NormalizedUsername = normalizedUsername,
            IsAvailable = !exists,
            Reason = exists ? "Username already exists." : null
        };
    }

    public async Task<UserActionResponse> ResendVerificationEmailAsync(string auth0UserId)
    {
        var user = await GetRequiredCurrentUserAsync(auth0UserId);
        EnsureActive(user);

        if (!user.EmailVerified)
            await _auth0ManagementService.SendVerificationEmailAsync(user.Auth0UserId);

        return new UserActionResponse(GenericVerificationMessage);
    }

    public async Task<UserActionResponse> SendPasswordResetEmailAsync(string auth0UserId)
    {
        var user = await GetRequiredCurrentUserAsync(auth0UserId);
        EnsureActive(user);

        var auth0Profile = await _auth0ManagementService.GetUserProfileAsync(user.Auth0UserId);
        user.SyncAuth0Profile(auth0Profile.Email, auth0Profile.EmailVerified, DateTime.UtcNow);
        await _dbContext.SaveChangesAsync();

        if (auth0Profile.HasPasswordResetIdentity && !string.IsNullOrWhiteSpace(auth0Profile.Email))
            await _auth0ManagementService.SendPasswordResetEmailAsync(auth0Profile.Email);

        return new UserActionResponse(GenericPasswordResetMessage);
    }

    public async Task<UserActionResponse> AnonymizeCurrentUserAsync(string auth0UserId)
    {
        var user = await GetRequiredCurrentUserAsync(auth0UserId);
        if (!user.IsDeleted)
        {
            var deletedAtUtc = DateTime.UtcNow;
            user.Anonymize(deletedAtUtc);
            PublishUserAnonymized(user, deletedAtUtc);
            await _dbContext.SaveChangesAsync();
        }

        return new UserActionResponse("Account deleted.");
    }

    public async Task<GetUserDTO> GetUserByIdAsync(Guid id)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            throw new NotFoundException($"User with ID {id} not found.");
        return new GetUserDTO(user);
    }

    public async Task<GetUserDTO> UpdateUserAsync(Guid id, UpdateUserProfileRequest request)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            throw new NotFoundException($"User with ID {id} not found.");

        var usernameChanged = ApplyProfileUpdate(user, request.Username, request.Firstname, request.Lastname, request.DiscordId, request.SteamId, request.RiotId);
        PublishProfileChanged(user, usernameChanged);

        await SaveProfileChangesAsync();

        return new GetUserDTO(user);
    }

    public async Task DeleteUserAsync(string username)
    {
        var normalizedUsername = UserProfileValidationHelper.NormalizeUsername(username);
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.NormalizedUsername == normalizedUsername);
        if (user == null)
            throw new NotFoundException($"User '{username}' not found.");

        var deletedAtUtc = DateTime.UtcNow;
        user.Anonymize(deletedAtUtc);
        PublishUserAnonymized(user, deletedAtUtc);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteUserByIdAsync(Guid id)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            throw new NotFoundException($"User with ID {id} not found.");

        var deletedAtUtc = DateTime.UtcNow;
        user.Anonymize(deletedAtUtc);
        PublishUserAnonymized(user, deletedAtUtc);
        await _dbContext.SaveChangesAsync();
    }

    private async Task<User> GetRequiredCurrentUserAsync(string auth0UserId)
    {
        var normalizedAuth0UserId = NormalizeAuth0UserId(auth0UserId);
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Auth0UserId == normalizedAuth0UserId);
        if (user == null)
            throw new NotFoundException("Current user profile was not found.");

        return user;
    }

    private async Task<User> CreateIncompleteUserAsync(string auth0UserId, Auth0ProfileSnapshot auth0Profile)
    {
        var normalizedAuth0UserId = NormalizeAuth0UserId(auth0UserId);
        if (await _dbContext.Users.AnyAsync(u => u.Auth0UserId == normalizedAuth0UserId))
            throw new ValidationException("Auth0 user already exists");

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Auth0UserId = normalizedAuth0UserId,
            Email = NormalizeEmailSnapshot(auth0Profile.Email),
            EmailVerified = auth0Profile.EmailVerified ?? false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.Users.Add(user);
        return user;
    }

    private bool ApplyProfileUpdate(
        User user,
        string username,
        string firstname,
        string lastname,
        string? discordId,
        string? steamId,
        string? riotId)
    {
        EnsureActive(user);

        var trimmedUsername = UserProfileValidationHelper.NormalizeRequiredText(username, "Username");
        if (!UserProfileValidationHelper.IsUsernameValid(trimmedUsername))
            throw new ValidationException("Username must be 3-32 alphanumeric characters.");

        if (UserProfileValidationHelper.IsReservedUsername(trimmedUsername))
            throw new ValidationException("Username is reserved.");

        var normalizedUsername = UserProfileValidationHelper.NormalizeUsername(trimmedUsername);

        if (_dbContext.Users.Any(u => u.NormalizedUsername == normalizedUsername && u.Id != user.Id && !u.IsDeleted))
            throw new ValidationException("Username already exists");

        var normalizedFirstname = UserProfileValidationHelper.NormalizeRequiredText(firstname, "Firstname");
        var normalizedLastname = UserProfileValidationHelper.NormalizeRequiredText(lastname, "Lastname");

        var usernameChanged = !string.Equals(user.NormalizedUsername, normalizedUsername, StringComparison.Ordinal);

        user.UpdateLocalProfile(
            trimmedUsername,
            normalizedUsername,
            normalizedFirstname,
            normalizedLastname,
            UserProfileValidationHelper.NormalizeOptionalPlatformId(discordId, "Discord ID"),
            UserProfileValidationHelper.NormalizeOptionalPlatformId(steamId, "Steam ID"),
            UserProfileValidationHelper.NormalizeOptionalPlatformId(riotId, "Riot ID"),
            DateTime.UtcNow);

        return usernameChanged;
    }

    private void PublishProfileChanged(User user, bool usernameChanged)
    {
        _moduleEventPublisher?.Publish(new UserProfileChangedIntegrationEvent(
            new UserId(user.Id),
            user.Username,
            user.DisplayName,
            user.IsDeleted,
            user.UpdatedAtUtc));

        if (usernameChanged && !string.IsNullOrWhiteSpace(user.Username))
        {
            _moduleEventPublisher?.Publish(new UsernameChangedIntegrationEvent(
                new UserId(user.Id),
                user.Username,
                user.UpdatedAtUtc));
        }
    }

    private void PublishUserAnonymized(User user, DateTime deletedAtUtc)
    {
        _moduleEventPublisher?.Publish(new UserAnonymizedIntegrationEvent(
            new UserId(user.Id),
            deletedAtUtc));
        _moduleEventPublisher?.Publish(new UserDeletedIntegrationEvent(
            new UserId(user.Id),
            deletedAtUtc));
        PublishProfileChanged(user, usernameChanged: true);
    }

    private async Task SaveProfileChangesAsync()
    {
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw new ValidationException("Username already exists");
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException?.Message.Contains("IX_Users_NormalizedUsername", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static string BuildUserSearchCursor(string normalizedQuery, UserSearchCandidate candidate)
    {
        var payload = new UserSearchCursor(normalizedQuery, candidate.RelevanceRank, candidate.NormalizedUsername, candidate.Id);
        return SearchCursorCodec.Encode(payload);
    }

    private static UserSearchCursor? DecodeUserSearchCursor(string? cursor, string normalizedQuery)
    {
        return SearchCursorCodec.Decode<UserSearchCursor>(
            cursor,
            normalizedQuery,
            payload =>
                !string.IsNullOrEmpty(payload.Query) &&
                payload.RelevanceRank is >= 0 and <= 2 &&
                !string.IsNullOrEmpty(payload.NormalizedUsername) &&
                payload.StableId != Guid.Empty,
            payload => payload.Query);
    }

    private sealed class UserSearchCandidate
    {
        public Guid Id { get; init; }
        public int RelevanceRank { get; init; }
        public required string NormalizedUsername { get; init; }
        public required string Username { get; init; }
    }

    private sealed record UserSearchCursor(string Query, int RelevanceRank, string NormalizedUsername, Guid StableId);

    private static void EnsureActive(User user)
    {
        if (user.IsDeleted)
            throw new DeletedAccountException();
    }

    private static string NormalizeAuth0UserId(string auth0UserId)
    {
        if (string.IsNullOrWhiteSpace(auth0UserId))
            throw new ValidationException("Auth0 user id is required.");

        return auth0UserId.Trim();
    }

    private static string? NormalizeEmailSnapshot(string? email)
    {
        return string.IsNullOrWhiteSpace(email) ? null : email.Trim();
    }
}
