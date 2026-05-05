using Auth.Module.Exceptions;
using Auth.Module.Services.Users;
using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.DTOs.UserDTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MercuriusNotFoundException = Mercurius.Shared.Exceptions.NotFoundException;
using MercuriusValidationException = Mercurius.Shared.Exceptions.ValidationException;

namespace Mercurius.LAN.API.Services.UserServices;

public class UserService : IUserService
{
    private readonly MercuriusDBContext _dbContext;
    private readonly UserProfileStore _userProfileStore;
    private readonly IAuthUserService _authUserService;

    public UserService(MercuriusDBContext dbContext, UserProfileStore userProfileStore, IAuthUserService authUserService)
    {
        _dbContext = dbContext;
        _userProfileStore = userProfileStore;
        _authUserService = authUserService;
    }

    public Task<IEnumerable<GetUserDTO>> GetAllUsersAsync() => _userProfileStore.GetAllAsync();

    public async Task<GetUserDTO> CreateUserAsync(CreateUserProfileRequest request)
    {
        if (await _userProfileStore.EmailExistsAsync(request.Email))
            throw new MercuriusValidationException("Email already exists");

        var userId = await _authUserService.CreateAuthUserAsync(request.Username, request.Password);
        await _userProfileStore.CreateAsync(request, userId);

        return await _userProfileStore.GetByIdAsync(userId)
            ?? throw new MercuriusNotFoundException($"User with ID {userId} not found.");
    }

    public async Task<GetUserDTO> GetUserByIdAsync(Guid id)
    {
        return await _userProfileStore.GetByIdAsync(id)
            ?? throw new MercuriusNotFoundException($"User with ID {id} not found.");
    }

    public async Task<GetUserDTO> UpdateUserAsync(Guid id, UpdateUserProfileRequest request)
    {
        var user = await _userProfileStore.GetByIdAsync(id);
        if (user == null)
            throw new MercuriusNotFoundException($"User with ID {id} not found.");

        if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase) &&
            await _userProfileStore.EmailExistsAsync(request.Email, id))
        {
            throw new MercuriusValidationException("Email already exists");
        }

        await _authUserService.UpdateUsernameAsync(id, request.Username);
        await _userProfileStore.UpdateAsync(id, request);

        return await _userProfileStore.GetByIdAsync(id)
            ?? throw new MercuriusNotFoundException($"User with ID {id} not found.");
    }

    public async Task DeleteUserAsync(string username)
    {
        var normalizedUsername = username.Normalize();
        var user = await _dbContext.AuthUsers.AsNoTracking().FirstOrDefaultAsync(authUser => authUser.Username == normalizedUsername);
        if (user == null)
            throw new Auth.Module.Exceptions.NotFoundException($"User '{username}' not found.");

        await _userProfileStore.DeleteAsync(user.Id);
        await _authUserService.DeleteAuthUserAsync(user.Id);
    }

    public async Task DeleteUserByIdAsync(Guid id)
    {
        await _userProfileStore.DeleteAsync(id);
        await _authUserService.DeleteAuthUserAsync(id);
    }

    public async Task AddRoleToUserAsync(string username, AddUserRoleRequest request)
    {
        var normalizedUsername = username.Normalize();
        var user = await _dbContext.AuthUsers.AsNoTracking().FirstOrDefaultAsync(authUser => authUser.Username == normalizedUsername);
        if (user == null)
            throw new Auth.Module.Exceptions.NotFoundException($"User '{username}' not found.");

        await _authUserService.AddRoleToUserAsync(user.Id, request.RoleName);
    }

    public async Task DeleteRoleFromUserAsync(string username, string roleName)
    {
        var normalizedUsername = username.Normalize();
        var user = await _dbContext.AuthUsers.AsNoTracking().FirstOrDefaultAsync(authUser => authUser.Username == normalizedUsername);
        if (user == null)
            throw new Auth.Module.Exceptions.NotFoundException($"User '{username}' not found.");

        await _authUserService.RemoveRoleFromUserAsync(user.Id, roleName);
    }

    public async Task ChangePasswordAsync(string username, ChangePasswordRequest request)
    {
        var normalizedUsername = username.Normalize();
        var user = await _dbContext.AuthUsers.AsNoTracking().FirstOrDefaultAsync(authUser => authUser.Username == normalizedUsername);
        if (user == null)
            throw new Auth.Module.Exceptions.NotFoundException($"User '{username}' not found.");

        await _authUserService.ChangePasswordAsync(user.Id, request.CurrentPassword, request.NewPassword);
    }

    public async Task SeedInitialUserAsync(IConfiguration configuration)
    {
        await _authUserService.SeedInitialUserAsync(configuration);

        var initialUserSection = configuration.GetSection("InitialUser");
        var username = initialUserSection["Username"];
        if (string.IsNullOrWhiteSpace(username))
            return;

        var normalizedUsername = username.Normalize();
        var password = initialUserSection["Password"] ?? string.Empty;
        var existingUser = await _dbContext.AuthUsers.AsNoTracking().FirstOrDefaultAsync(authUser => authUser.Username == normalizedUsername);
        if (existingUser == null)
            return;

        var existingProfile = await _userProfileStore.GetByIdAsync(existingUser.Id);
        if (existingProfile != null)
            return;

        await _userProfileStore.CreateAsync(new CreateUserProfileRequest
        {
            Username = normalizedUsername,
            Password = password,
            Firstname = normalizedUsername,
            Lastname = normalizedUsername,
            Email = $"{normalizedUsername}@local.invalid"
        }, existingUser.Id);
    }
}
