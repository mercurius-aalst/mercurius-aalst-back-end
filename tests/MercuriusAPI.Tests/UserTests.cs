using Auth.Module.Models;
using Mercurius.LAN.API.DTOs.UserDTOs;
using Mercurius.LAN.API.Services.UserServices;
using Mercurius.Shared.Exceptions;
using Microsoft.Extensions.Configuration;

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

        var dto = new GetUserDTO(user, ["admin", "captain"]);

        Assert.Equal(user.Id, dto.Id);
        Assert.Equal("playerone", dto.Username);
        Assert.Equal("Player", dto.Firstname);
        Assert.Equal("One", dto.Lastname);
        Assert.Equal("playerone@test.com", dto.Email);
        Assert.Equal("discord-1", dto.DiscordId);
        Assert.Equal("steam-1", dto.SteamId);
        Assert.Equal("riot-1", dto.RiotId);
        Assert.Equal("Player One", dto.DisplayName);
        Assert.Equal(["admin", "captain"], dto.Roles);
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
            Username = "ValidUser",
            Password = "Strong!123",
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
    public async Task CreateUserAsync_RejectsWeakPassword()
    {
        var service = new UserValidationService(new RecordingUserService());
        var request = new CreateUserProfileRequest
        {
            Username = "ValidUser",
            Password = "weak",
            Firstname = "Player",
            Lastname = "One",
            Email = "playerone@test.com"
        };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => service.CreateUserAsync(request));

        Assert.Contains("Password", exception.Message);
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
        public GetUserDTO CreatedUser { get; } = new(new User
        {
            Id = Guid.NewGuid(),
            Username = "ValidUser",
            Firstname = "Player",
            Lastname = "One",
            Email = "playerone@test.com"
        }, []);

        public Task<GetUserDTO> CreateUserAsync(CreateUserProfileRequest request)
        {
            LastCreateRequest = request;
            return Task.FromResult(CreatedUser);
        }

        public Task DeleteUserAsync(string username) => Task.CompletedTask;
        public Task DeleteUserByIdAsync(Guid id) => Task.CompletedTask;
        public Task AddRoleToUserAsync(string username, AddUserRoleRequest request) => Task.CompletedTask;
        public Task ChangePasswordAsync(string username, ChangePasswordRequest newPassword) => Task.CompletedTask;
        public Task<IEnumerable<GetUserDTO>> GetAllUsersAsync() => Task.FromResult<IEnumerable<GetUserDTO>>([]);
        public Task<GetUserDTO> GetUserByIdAsync(Guid id) => Task.FromResult(CreatedUser);
        public Task<GetUserDTO> UpdateUserAsync(Guid id, UpdateUserProfileRequest request) => Task.FromResult(CreatedUser);
        public Task DeleteRoleFromUserAsync(string username, string roleName) => Task.CompletedTask;
        public Task SeedInitialUserAsync(IConfiguration configuration) => Task.CompletedTask;
    }


    [Fact]
    public void SocialFirstUser_HasNoLocalPasswordByDefault()
    {
        var user = new AuthUser
        {
            Username = "social-user"
        };

        Assert.Null(user.PasswordHash);
        Assert.Null(user.Salt);
    }
}
