using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.DTOs.Auth;
using Mercurius.LAN.API.Exceptions;
using Mercurius.LAN.API.Models;
using Mercurius.LAN.API.Services.Auth0;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.LAN.API.Services.UserServices;

public class UserService : IUserService
{
    private const string GenericVerificationMessage = "If verification is available for this account, a verification email has been sent.";
    private const string GenericPasswordResetMessage = "If password reset is available for this account, a password reset email has been sent.";
    private readonly MercuriusDBContext _dbContext;
    private readonly IAuth0ManagementService _auth0ManagementService;

    public UserService(MercuriusDBContext dbContext, IAuth0ManagementService auth0ManagementService)
    {
        _dbContext = dbContext;
        _auth0ManagementService = auth0ManagementService;
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

        ApplyProfileUpdate(user, request.Username, request.Firstname, request.Lastname, request.DiscordId, request.SteamId, request.RiotId);

        await SaveProfileChangesAsync();
        return new GetUserDTO(user);
    }

    public async Task<GetUserDTO> CompleteProfileAsync(string auth0UserId, CompleteUserProfileRequest request)
    {
        var user = await GetOrCreateCurrentUserAsync(auth0UserId);
        EnsureActive(user);

        ApplyProfileUpdate(user, request.Username, request.Firstname, request.Lastname, request.DiscordId, request.SteamId, request.RiotId);

        await SaveProfileChangesAsync();
        return new GetUserDTO(user);
    }

    public async Task<CurrentUserProfileResponse> GetCurrentUserAsync(string auth0UserId)
    {
        var user = await GetOrCreateCurrentUserAsync(auth0UserId);
        EnsureActive(user);

        await _dbContext.SaveChangesAsync();

        return new CurrentUserProfileResponse(user.IsComplete, new GetUserDTO(user));
    }

    public async Task<PublicUserProfileDTO> GetPublicUserProfileByUsernameAsync(string username, bool includePlatformIds)
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
                DiscordId = includePlatformIds ? u.DiscordId : null,
                SteamId = includePlatformIds ? u.SteamId : null,
                RiotId = includePlatformIds ? u.RiotId : null
            })
            .FirstOrDefaultAsync();

        if (profile == null)
            throw new NotFoundException($"User '{trimmedUsername}' not found.");

        return profile;
    }

    public async Task<GetUserDTO> UpdateCurrentUserAsync(string auth0UserId, UpdateUserProfileRequest request)
    {
        var user = await GetRequiredCurrentUserAsync(auth0UserId);
        EnsureActive(user);

        ApplyProfileUpdate(user, request.Username, request.Firstname, request.Lastname, request.DiscordId, request.SteamId, request.RiotId);

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
            user.Anonymize(DateTime.UtcNow);
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

        ApplyProfileUpdate(user, request.Username, request.Firstname, request.Lastname, request.DiscordId, request.SteamId, request.RiotId);

        await SaveProfileChangesAsync();

        return new GetUserDTO(user);
    }

    public async Task DeleteUserAsync(string username)
    {
        var normalizedUsername = UserProfileValidationHelper.NormalizeUsername(username);
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.NormalizedUsername == normalizedUsername);
        if (user == null)
            throw new NotFoundException($"User '{username}' not found.");

        user.Anonymize(DateTime.UtcNow);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteUserByIdAsync(Guid id)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            throw new NotFoundException($"User with ID {id} not found.");

        user.Anonymize(DateTime.UtcNow);
        await _dbContext.SaveChangesAsync();
    }

    private async Task<User> GetOrCreateCurrentUserAsync(string auth0UserId)
    {
        var normalizedAuth0UserId = NormalizeAuth0UserId(auth0UserId);
        var auth0Profile = await _auth0ManagementService.GetUserProfileAsync(normalizedAuth0UserId);
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Auth0UserId == normalizedAuth0UserId);

        if (user == null)
            return await CreateIncompleteUserAsync(normalizedAuth0UserId, auth0Profile);

        if (!user.IsDeleted)
            user.SyncAuth0Profile(auth0Profile.Email, auth0Profile.EmailVerified, DateTime.UtcNow);

        return user;
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

    private void ApplyProfileUpdate(
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

        user.UpdateLocalProfile(
            trimmedUsername,
            normalizedUsername,
            normalizedFirstname,
            normalizedLastname,
            UserProfileValidationHelper.NormalizeOptionalPlatformId(discordId, "Discord ID"),
            UserProfileValidationHelper.NormalizeOptionalPlatformId(steamId, "Steam ID"),
            UserProfileValidationHelper.NormalizeOptionalPlatformId(riotId, "Riot ID"),
            DateTime.UtcNow);
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
