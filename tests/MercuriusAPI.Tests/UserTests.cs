using Mercurius.LAN.API.DTOs.Auth;
using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.Exceptions;
using Mercurius.LAN.API.Models;
using Mercurius.LAN.API.Services.Auth0;
using Mercurius.LAN.API.Services.UserServices;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.LAN.API.Tests;

public class UserTests
{
    [Fact]
    public void UpdateLocalProfile_UpdatesAppOwnedProfileFields()
    {
        var user = new User
        {
            Username = "user",
            NormalizedUsername = "user",
            Firstname = "OldFirst",
            Lastname = "OldLast",
            Email = "old@test.com"
        };
        var updatedAtUtc = DateTime.UtcNow;

        user.UpdateLocalProfile("NewUser", "newuser", "NewFirst", "NewLast", "discord", "steam", "riot", updatedAtUtc);

        Assert.Equal("NewUser", user.Username);
        Assert.Equal("newuser", user.NormalizedUsername);
        Assert.Equal("NewFirst", user.Firstname);
        Assert.Equal("NewLast", user.Lastname);
        Assert.Equal("old@test.com", user.Email);
        Assert.Equal("discord", user.DiscordId);
        Assert.Equal("steam", user.SteamId);
        Assert.Equal("riot", user.RiotId);
        Assert.Equal("NewFirst NewLast", user.DisplayName);
        Assert.Equal(updatedAtUtc, user.UpdatedAtUtc);
    }

    [Fact]
    public void SyncAuth0Profile_UpdatesEmailSnapshotOnly()
    {
        var user = new User
        {
            Username = "user",
            Email = "old@test.com",
            EmailVerified = false
        };
        var updatedAtUtc = DateTime.UtcNow;

        user.SyncAuth0Profile("new@test.com", true, updatedAtUtc);

        Assert.Equal("new@test.com", user.Email);
        Assert.True(user.EmailVerified);
        Assert.Equal("user", user.Username);
        Assert.Equal(updatedAtUtc, user.UpdatedAtUtc);
    }

    [Fact]
    public void GetUserDTO_MapsProfileFieldsAndDisplayName()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Auth0UserId = "auth0|123",
            Username = "PlayerOne",
            NormalizedUsername = "playerone",
            Firstname = "Player",
            Lastname = "One",
            Email = "playerone@test.com",
            EmailVerified = true,
            DiscordId = "discord-1",
            SteamId = "steam-1",
            RiotId = "riot-1"
        };

        var dto = new GetUserDTO(user);

        Assert.Equal(user.Id, dto.Id);
        Assert.Equal("PlayerOne", dto.Username);
        Assert.Equal("Player", dto.Firstname);
        Assert.Equal("One", dto.Lastname);
        Assert.Equal("playerone@test.com", dto.Email);
        Assert.True(dto.EmailVerified);
        Assert.Equal("discord-1", dto.DiscordId);
        Assert.Equal("steam-1", dto.SteamId);
        Assert.Equal("riot-1", dto.RiotId);
        Assert.Equal("Player One", dto.DisplayName);
    }

    [Fact]
    public void DisplayName_FallsBackToUsername_WhenNameIsMissing()
    {
        var user = new User
        {
            Username = "fallback-user",
            Firstname = "",
            Lastname = " "
        };

        Assert.Equal("fallback-user", user.DisplayName);
    }

    [Fact]
    public void IsComplete_RequiresProfileFieldsAndActiveAccount()
    {
        var user = new User
        {
            Username = "playerone",
            NormalizedUsername = "playerone",
            Firstname = "Player",
            Lastname = "One"
        };

        Assert.True(user.IsComplete);

        user.IsDeleted = true;

        Assert.False(user.IsComplete);
    }

    [Theory]
    [InlineData(" PlayerOne ", "playerone")]
    [InlineData("ADMIN", "admin")]
    public void NormalizeUsername_TrimsAndLowercasesInvariantly(string username, string expected)
    {
        Assert.Equal(expected, UserProfileValidationHelper.NormalizeUsername(username));
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("user-name")]
    [InlineData("thisusernameiswaytoolongtobeaccepted")]
    public void IsUsernameValid_RejectsInvalidUsernames(string username)
    {
        Assert.False(UserProfileValidationHelper.IsUsernameValid(username));
    }

    [Fact]
    public void IsReservedUsername_RejectsReservedNames()
    {
        Assert.True(UserProfileValidationHelper.IsReservedUsername(" Admin "));
    }

    [Fact]
    public void Anonymize_ClearsPersonalFieldsAndKeepsHistoricalIdentity()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Auth0UserId = "auth0|123",
            Username = "PlayerOne",
            NormalizedUsername = "playerone",
            Firstname = "Player",
            Lastname = "One",
            Email = "playerone@test.com",
            EmailVerified = true,
            DiscordId = "discord-1",
            SteamId = "steam-1",
            RiotId = "riot-1"
        };
        var deletedAtUtc = DateTime.UtcNow;

        user.Anonymize(deletedAtUtc);

        Assert.True(user.IsDeleted);
        Assert.Equal("auth0|123", user.Auth0UserId);
        Assert.StartsWith("deleted-user-", user.Username);
        Assert.Equal(user.Username, user.NormalizedUsername);
        Assert.Null(user.Firstname);
        Assert.Null(user.Lastname);
        Assert.Null(user.Email);
        Assert.False(user.EmailVerified);
        Assert.Null(user.DiscordId);
        Assert.Null(user.SteamId);
        Assert.Null(user.RiotId);
        Assert.Equal(deletedAtUtc, user.DeletedAtUtc);
    }

    [Fact]
    public async Task CreateUserAsync_ForwardsValidProfileRequest()
    {
        var inner = new RecordingUserService();
        var service = new UserValidationService(inner);
        var request = new CreateUserProfileRequest
        {
            Auth0UserId = "auth0|123",
            Username = "ValidUser",
            Firstname = "Player",
            Lastname = "One",
            Email = "playerone@test.com",
            DiscordId = "discord-1"
        };

        var result = await service.CreateUserAsync(request);

        Assert.Same(inner.CreatedUser, result);
        Assert.NotNull(inner.LastCreateRequest);
        Assert.Equal("ValidUser", inner.LastCreateRequest!.Username);
        Assert.Equal("playerone@test.com", inner.LastCreateRequest.Email);
    }

    [Fact]
    public async Task CreateUserAsync_RejectsMissingAuth0UserId()
    {
        var service = new UserValidationService(new RecordingUserService());
        var request = new CreateUserProfileRequest
        {
            Auth0UserId = "",
            Username = "ValidUser",
            Firstname = "Player",
            Lastname = "One"
        };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.CreateUserAsync(request));

        Assert.Contains("Auth0 user id", exception.Message);
    }

    [Fact]
    public async Task CompleteProfileAsync_ForwardsSubjectAndProfileRequest()
    {
        var inner = new RecordingUserService();
        var service = new UserValidationService(inner);
        var request = new CompleteUserProfileRequest
        {
            Username = "ValidUser",
            Firstname = "Player",
            Lastname = "One"
        };

        var result = await service.CompleteProfileAsync("auth0|123", request);

        Assert.Same(inner.CreatedUser, result);
        Assert.Equal("auth0|123", inner.LastCompleteSubject);
        Assert.Same(request, inner.LastCompleteRequest);
    }

    [Fact]
    public async Task UpdateUserAsync_RejectsInvalidIdentityPayload()
    {
        var service = new UserValidationService(new RecordingUserService());
        var request = new UpdateUserProfileRequest
        {
            Username = "x",
            Firstname = "Player",
            Lastname = "One"
        };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.UpdateUserAsync(Guid.NewGuid(), request));

        Assert.Contains("Username", exception.Message);
    }

    [Fact]
    public async Task UpdateCurrentUserAsync_ForwardsValidProfileRequest()
    {
        var inner = new RecordingUserService();
        var service = new UserValidationService(inner);
        var request = new UpdateUserProfileRequest
        {
            Username = "ValidUser",
            Firstname = "Player",
            Lastname = "One"
        };

        var result = await service.UpdateCurrentUserAsync("auth0|123", request);

        Assert.Same(inner.CreatedUser, result);
        Assert.Equal("auth0|123", inner.LastUpdateCurrentSubject);
        Assert.Same(request, inner.LastUpdateCurrentRequest);
    }

    [Fact]
    public async Task DeleteUserByIdAsync_RejectsEmptyIds()
    {
        var service = new UserValidationService(new RecordingUserService());

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.DeleteUserByIdAsync(Guid.Empty));

        Assert.Equal("Invalid user ID.", exception.Message);
    }

    [Fact]
    public void Auth0LinkedUser_StoresExternalUserId()
    {
        var user = new User
        {
            Auth0UserId = "auth0|social-user",
            Username = "social-user",
            Email = "social@test.com"
        };

        Assert.Equal("auth0|social-user", user.Auth0UserId);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_DoesNotTriggerReset_ForSocialOnlyIdentity()
    {
        await using var dbContext = CreateDbContext();
        var user = CreateStoredUser("google-oauth2|123", "shared@example.com");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var auth0ManagementService = new RecordingAuth0ManagementService(
            new Auth0ProfileSnapshot("shared@example.com", true, false));
        var service = new UserService(dbContext, auth0ManagementService);

        var response = await service.SendPasswordResetEmailAsync(user.Auth0UserId);

        Assert.Equal("If password reset is available for this account, a password reset email has been sent.", response.Message);
        Assert.Null(auth0ManagementService.LastPasswordResetEmail);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_TriggersReset_ForDatabaseIdentity()
    {
        await using var dbContext = CreateDbContext();
        var user = CreateStoredUser("auth0|123", "shared@example.com");
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var auth0ManagementService = new RecordingAuth0ManagementService(
            new Auth0ProfileSnapshot("shared@example.com", true, true));
        var service = new UserService(dbContext, auth0ManagementService);

        await service.SendPasswordResetEmailAsync(user.Auth0UserId);

        Assert.Equal("shared@example.com", auth0ManagementService.LastPasswordResetEmail);
    }

    private static MercuriusDBContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MercuriusDBContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MercuriusDBContext(options);
    }

    private static User CreateStoredUser(string auth0UserId, string email)
    {
        var now = DateTime.UtcNow;
        return new User
        {
            Id = Guid.NewGuid(),
            Auth0UserId = auth0UserId,
            Username = "PlayerOne",
            NormalizedUsername = "playerone",
            Firstname = "Player",
            Lastname = "One",
            Email = email,
            EmailVerified = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    private sealed class RecordingUserService : IUserService
    {
        public CreateUserProfileRequest? LastCreateRequest { get; private set; }
        public string? LastCompleteSubject { get; private set; }
        public CompleteUserProfileRequest? LastCompleteRequest { get; private set; }
        public string? LastUpdateCurrentSubject { get; private set; }
        public UpdateUserProfileRequest? LastUpdateCurrentRequest { get; private set; }
        public GetUserDTO CreatedUser { get; } = new(new User
        {
            Id = Guid.NewGuid(),
            Auth0UserId = "auth0|123",
            Username = "ValidUser",
            NormalizedUsername = "validuser",
            Firstname = "Player",
            Lastname = "One",
            Email = "playerone@test.com"
        });

        public Task<GetUserDTO> CreateUserAsync(CreateUserProfileRequest request)
        {
            LastCreateRequest = request;
            return Task.FromResult(CreatedUser);
        }

        public Task<GetUserDTO> CompleteProfileAsync(string auth0UserId, CompleteUserProfileRequest request)
        {
            LastCompleteSubject = auth0UserId;
            LastCompleteRequest = request;
            return Task.FromResult(CreatedUser);
        }

        public Task<CurrentUserProfileResponse> GetCurrentUserAsync(string auth0UserId)
        {
            return Task.FromResult(new CurrentUserProfileResponse(true, CreatedUser));
        }

        public Task<GetUserDTO> UpdateCurrentUserAsync(string auth0UserId, UpdateUserProfileRequest request)
        {
            LastUpdateCurrentSubject = auth0UserId;
            LastUpdateCurrentRequest = request;
            return Task.FromResult(CreatedUser);
        }

        public Task<UsernameAvailabilityResponse> CheckUsernameAvailabilityAsync(string auth0UserId, string username)
        {
            return Task.FromResult(new UsernameAvailabilityResponse { Username = username, IsAvailable = true });
        }

        public Task<UserActionResponse> ResendVerificationEmailAsync(string auth0UserId)
        {
            return Task.FromResult(new UserActionResponse("verification"));
        }

        public Task<UserActionResponse> SendPasswordResetEmailAsync(string auth0UserId)
        {
            return Task.FromResult(new UserActionResponse("password"));
        }

        public Task<UserActionResponse> AnonymizeCurrentUserAsync(string auth0UserId)
        {
            return Task.FromResult(new UserActionResponse("deleted"));
        }

        public Task DeleteUserAsync(string username) => Task.CompletedTask;
        public Task DeleteUserByIdAsync(Guid id) => Task.CompletedTask;
        public Task<IEnumerable<GetUserDTO>> GetAllUsersAsync() => Task.FromResult<IEnumerable<GetUserDTO>>([]);
        public Task<GetUserDTO> GetUserByIdAsync(Guid id) => Task.FromResult(CreatedUser);
        public Task<GetUserDTO> UpdateUserAsync(Guid id, UpdateUserProfileRequest request) => Task.FromResult(CreatedUser);
    }

    private sealed class RecordingAuth0ManagementService : IAuth0ManagementService
    {
        private readonly Auth0ProfileSnapshot _profileSnapshot;

        public RecordingAuth0ManagementService(Auth0ProfileSnapshot profileSnapshot)
        {
            _profileSnapshot = profileSnapshot;
        }

        public string? LastPasswordResetEmail { get; private set; }

        public Task<Auth0ProfileSnapshot> GetUserProfileAsync(string auth0UserId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_profileSnapshot);
        }

        public Task SendVerificationEmailAsync(string auth0UserId, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SendPasswordResetEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            LastPasswordResetEmail = email;
            return Task.CompletedTask;
        }
    }
}
