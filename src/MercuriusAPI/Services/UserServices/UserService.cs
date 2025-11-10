using MercuriusAPI.Data;
using MercuriusAPI.DTOs.Auth;
using MercuriusAPI.Exceptions;
using MercuriusAPI.Models.Auth;
using Microsoft.EntityFrameworkCore;
using MercuriusAPI.Services.Auth;
using MercuriusAPI.Models;

namespace MercuriusAPI.Services.UserServices;

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

    public async Task<GetUserDTO> GetUserByIdAsync(int id)
    {
        var user = await _dbContext.Users.Include(u => u.Roles).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            throw new NotFoundException($"User with ID {id} not found.");
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
