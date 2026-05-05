using Mercurius.LAN.API.DTOs.Auth;
using Mercurius.LAN.API.Exceptions;
using Mercurius.LAN.API.Models;
using Mercurius.LAN.API.Services.UserServices;

namespace Mercurius.LAN.API.Tests;

public class UserTests
{
    [Fact]
    public void UpdateProfile_UpdatesCanonicalIdentityFields()
    {
        var user = new User
        {
            Username = "user",
            Firstname = "OldFirst",
            Lastname = "OldLast",
            Email = "old@test.com"
        };

        user.UpdateProfile("NewFirst", "NewLast", "new@test.com", "discord", "steam", "riot");

        Assert.Equal("NewFirst", user.Firstname);
        Assert.Equal("NewLast", user.Lastname);
        Assert.Equal("new@test.com", user.Email);
        Assert.Equal("discord", user.DiscordId);
        Assert.Equal("steam", user.SteamId);
        Assert.Equal("riot", user.RiotId);
        Assert.Equal("NewFirst NewLast", user.DisplayName);
        Assert.Equal("user", user.Username);
    }

    [Fact]
    public void GetUserDTO_MapsProfileFieldsAndDisplayName()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "playerone",
            Firstname = "Player",
            Lastname = "One",
            Email = "playerone@test.com",
            DiscordId = "discord-1",
            SteamId = "steam-1",
            RiotId = "riot-1"
        };

        var dto = new GetUserDTO(user);

        Assert.Equal(user.Id, dto.Id);
        Assert.Equal("playerone", dto.Username);
        Assert.Equal("Player", dto.Firstname);
        Assert.Equal("One", dto.Lastname);
        Assert.Equal("playerone@test.com", dto.Email);
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
    public async Task CreateUserAsync_ForwardsValidProfileRequest()
    {
        var inner = new RecordingUserService();
        var service = new UserValidationService(inner);
        var request = new CreateUserProfileRequest
        {
            Auth0Subject = "auth0|123",
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
    public async Task CreateUserAsync_RejectsMissingAuth0Subject()
    {
        var service = new UserValidationService(new RecordingUserService());
        var request = new CreateUserProfileRequest
        {
            Auth0Subject = "",
            Username = "ValidUser",
            Firstname = "Player",
            Lastname = "One",
            Email = "playerone@test.com"
        };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.CreateUserAsync(request));

        Assert.Contains("Auth0 subject", exception.Message);
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
            Lastname = "One",
            Email = "playerone@test.com"
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
            Lastname = "One",
            Email = "playerone@test.com"
        };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.UpdateUserAsync(Guid.NewGuid(), request));

        Assert.Contains("Username", exception.Message);
    }

    [Fact]
    public async Task DeleteUserByIdAsync_RejectsNonPositiveIds()
    {
        var service = new UserValidationService(new RecordingUserService());

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.DeleteUserByIdAsync(Guid.Empty));

        Assert.Equal("Invalid user ID.", exception.Message);
    }

    private sealed class RecordingUserService : IUserService
    {
        public CreateUserProfileRequest? LastCreateRequest { get; private set; }
        public string? LastCompleteSubject { get; private set; }
        public CompleteUserProfileRequest? LastCompleteRequest { get; private set; }
        public GetUserDTO CreatedUser { get; } = new(new User
        {
            Id = Guid.NewGuid(),
            Auth0Subject = "auth0|123",
            Username = "ValidUser",
            Firstname = "Player",
            Lastname = "One",
            Email = "playerone@test.com"
        });

        public Task<GetUserDTO> CreateUserAsync(CreateUserProfileRequest request)
        {
            LastCreateRequest = request;
            return Task.FromResult(CreatedUser);
        }

        public Task<GetUserDTO> CompleteProfileAsync(string auth0Subject, CompleteUserProfileRequest request)
        {
            LastCompleteSubject = auth0Subject;
            LastCompleteRequest = request;
            return Task.FromResult(CreatedUser);
        }

        public Task<CurrentUserProfileResponse> GetCurrentUserAsync(string auth0Subject)
        {
            return Task.FromResult(new CurrentUserProfileResponse(true, CreatedUser));
        }

        public Task DeleteUserAsync(string username) => Task.CompletedTask;
        public Task DeleteUserByIdAsync(Guid id) => Task.CompletedTask;
        public Task<IEnumerable<GetUserDTO>> GetAllUsersAsync() => Task.FromResult<IEnumerable<GetUserDTO>>([]);
        public Task<GetUserDTO> GetUserByIdAsync(Guid id) => Task.FromResult(CreatedUser);
        public Task<GetUserDTO> UpdateUserAsync(Guid id, UpdateUserProfileRequest request) => Task.FromResult(CreatedUser);
    }


    [Fact]
    public void Auth0LinkedUser_StoresExternalSubject()
    {
        var user = new User
        {
            Auth0Subject = "auth0|social-user",
            Username = "social-user",
            Email = "social@test.com"
        };

        Assert.Equal("auth0|social-user", user.Auth0Subject);
    }
}
