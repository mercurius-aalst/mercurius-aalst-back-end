using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.DTOs.Auth;
using Mercurius.LAN.API.Exceptions;
using Mercurius.LAN.API.Models.Auth;
using Microsoft.EntityFrameworkCore;
using Mercurius.LAN.API.Services.Auth;
using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.Services.UserServices;

public class UserService : IUserService
{
    private readonly MercuriusDBContext _dbContext;

    public UserService(MercuriusDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<GetUserDTO>> GetAllUsersAsync()
    {
        return await _dbContext.Users.Include(u => u.Roles).Select(u => new GetUserDTO(u)).ToListAsync();
    }

    public async Task<GetUserDTO> CreateUserAsync(CreateUserProfileRequest request)
    {
        var normalizedUsername = request.Username.Normalize();

        if (await _dbContext.Users.AnyAsync(u => u.Username == normalizedUsername))
            throw new ValidationException("Username already exists");

        if (await _dbContext.Users.AnyAsync(u => u.Email == request.Email))
            throw new ValidationException("Email already exists");

        PasswordHelper.CreatePasswordHash(request.Password, out var hash, out var salt);

        var user = new User
        {
            Username = normalizedUsername,
            PasswordHash = hash,
            Salt = salt
        };

        user.UpdateProfile(request.Firstname, request.Lastname, request.Email, request.DiscordId, request.SteamId, request.RiotId);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return new GetUserDTO(user);
    }

    public async Task<GetUserDTO> GetUserByIdAsync(Guid id)
    {
        var user = await _dbContext.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            throw new NotFoundException($"User with ID {id} not found.");
        return new GetUserDTO(user);
    }

    public async Task<GetUserDTO> UpdateUserAsync(Guid id, UpdateUserProfileRequest request)
    {
        var user = await _dbContext.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            throw new NotFoundException($"User with ID {id} not found.");

        var normalizedUsername = request.Username.Normalize();

        if (!string.Equals(user.Username, normalizedUsername, StringComparison.Ordinal) &&
            await _dbContext.Users.AnyAsync(u => u.Username == normalizedUsername && u.Id != id))
        {
            throw new ValidationException("Username already exists");
        }

        if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase) &&
            await _dbContext.Users.AnyAsync(u => u.Email == request.Email && u.Id != id))
        {
            throw new ValidationException("Email already exists");
        }

        user.Username = normalizedUsername;
        user.UpdateProfile(request.Firstname, request.Lastname, request.Email, request.DiscordId, request.SteamId, request.RiotId);

        await _dbContext.SaveChangesAsync();

        return new GetUserDTO(user);
    }

    public async Task DeleteUserAsync(string username)
    {
        var normalizedUsername = username.Normalize();
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username == normalizedUsername);
        if (user == null)
            throw new NotFoundException($"User '{username}' not found.");

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteUserByIdAsync(Guid id)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            throw new NotFoundException($"User with ID {id} not found.");

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();
    }

    public async Task AddRoleToUserAsync(string username, AddUserRoleRequest request)
    {
        var normalizedUsername = username.Normalize();
        var user = await _dbContext.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Username == normalizedUsername);
        if (user == null)
            throw new NotFoundException($"User '{username}' not found.");
        var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == request.RoleName);
        if (role == null)
        {
            role = new Role { Name = request.RoleName };
            _dbContext.Roles.Add(role);
            await _dbContext.SaveChangesAsync();
        }
        if (!user.Roles.Contains(role))
        {
            user.Roles.Add(role);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task DeleteRoleFromUserAsync(string username, string roleName)
    {
        var normalizedUsername = username.Normalize();
        var user = await _dbContext.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Username == normalizedUsername);
        if (user == null)
            throw new NotFoundException($"User '{username}' not found.");
        var role = user.Roles.FirstOrDefault(r => r.Name == roleName);
        if (role == null)
            throw new NotFoundException($"Role '{roleName}' not found for user '{username}'.");
        user.Roles.Remove(role);
        await _dbContext.SaveChangesAsync();
    }

    public Task ChangePasswordAsync(string username, ChangePasswordRequest request)
    {
        var normalizedUsername = username.Normalize();
        var user = _dbContext.Users.FirstOrDefault(u => u.Username == normalizedUsername);
        if (user == null)
            throw new NotFoundException($"User '{username}' not found.");
        PasswordHelper.CreatePasswordHash(request.NewPassword, out var hash, out var salt);
        user.PasswordHash = hash;
        user.Salt = salt;
        _dbContext.Users.Update(user);
        return _dbContext.SaveChangesAsync();
    }

    public async Task SeedInitialUserAsync(IConfiguration configuration)
    {
        // Only seed if there are no users in the database
        if (await _dbContext.Users.AnyAsync())
            return;
        var initialUserSection = configuration.GetSection("InitialUser");
        var username = initialUserSection["Username"];
        var password = initialUserSection["Password"];
        var roleName = initialUserSection["Role"];
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(roleName))
            return;
        var normalizedUsername = username.Normalize();
        PasswordHelper.CreatePasswordHash(password, out var hash, out var salt);
        var user = new User
        {
            Username = normalizedUsername,
            PasswordHash = hash,
            Salt = salt,
            Roles = new List<Role>()
        };
        var role = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (role == null)
        {
            role = new Role { Name = roleName };
            _dbContext.Roles.Add(role);
            await _dbContext.SaveChangesAsync();
        }
        user.Roles.Add(role);
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
    }
}
