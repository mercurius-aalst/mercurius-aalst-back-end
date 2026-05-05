using Auth.Module.Exceptions;
using Auth.Module.Models;
using Auth.Module.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Auth.Module.Services.Users;

public class AuthUserService : IAuthUserService
{
    private readonly IAuthDbContext _dbContext;

    public AuthUserService(IAuthDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Guid> CreateAuthUserAsync(string username, string password)
    {
        var normalizedUsername = username.Normalize();

        if (await _dbContext.AuthUsers.AnyAsync(u => u.Username == normalizedUsername))
            throw new ValidationException("Username already exists");

        PasswordHelper.CreatePasswordHash(password, out var hash, out var salt);

        var user = new AuthUser
        {
            Username = normalizedUsername,
            PasswordHash = hash,
            Salt = salt
        };

        _dbContext.AuthUsers.Add(user);
        await _dbContext.SaveChangesAsync();
        return user.Id;
    }

    public async Task<Guid> CreateExternalAuthUserAsync(string username)
    {
        var normalizedUsername = username.Normalize();

        if (await _dbContext.AuthUsers.AnyAsync(u => u.Username == normalizedUsername))
            throw new ValidationException("Username already exists");

        var user = new AuthUser
        {
            Username = normalizedUsername
        };

        _dbContext.AuthUsers.Add(user);
        await _dbContext.SaveChangesAsync();
        return user.Id;
    }

    public async Task<GetAuthUserDTO> GetAuthUserByIdAsync(Guid id)
    {
        var user = await _dbContext.AuthUsers
            .Include(authUser => authUser.Roles)
            .FirstOrDefaultAsync(authUser => authUser.Id == id)
            ?? throw new NotFoundException($"User with ID {id} not found.");

        return new GetAuthUserDTO(user);
    }

    public async Task UpdateUsernameAsync(Guid id, string username)
    {
        var normalizedUsername = username.Normalize();
        var user = await _dbContext.AuthUsers.FirstOrDefaultAsync(authUser => authUser.Id == id)
            ?? throw new NotFoundException($"User with ID {id} not found.");

        if (!string.Equals(user.Username, normalizedUsername, StringComparison.Ordinal) &&
            await _dbContext.AuthUsers.AnyAsync(authUser => authUser.Username == normalizedUsername && authUser.Id != id))
        {
            throw new ValidationException("Username already exists");
        }

        user.Username = normalizedUsername;
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAuthUserAsync(Guid id)
    {
        var user = await _dbContext.AuthUsers.FirstOrDefaultAsync(authUser => authUser.Id == id)
            ?? throw new NotFoundException($"User with ID {id} not found.");

        _dbContext.AuthUsers.Remove(user);
        await _dbContext.SaveChangesAsync();
    }

    public async Task AddRoleToUserAsync(Guid id, string roleName)
    {
        var user = await _dbContext.AuthUsers
            .Include(authUser => authUser.Roles)
            .FirstOrDefaultAsync(authUser => authUser.Id == id)
            ?? throw new NotFoundException($"User with ID {id} not found.");

        var role = await _dbContext.Roles.FirstOrDefaultAsync(existingRole => existingRole.Name == roleName);
        if (role == null)
        {
            role = new Role { Name = roleName };
            _dbContext.Roles.Add(role);
            await _dbContext.SaveChangesAsync();
        }

        if (user.Roles.Any(existingRole => existingRole.Name == role.Name))
            return;

        user.Roles.Add(role);
        await _dbContext.SaveChangesAsync();
    }

    public async Task RemoveRoleFromUserAsync(Guid id, string roleName)
    {
        var user = await _dbContext.AuthUsers
            .Include(authUser => authUser.Roles)
            .FirstOrDefaultAsync(authUser => authUser.Id == id)
            ?? throw new NotFoundException($"User with ID {id} not found.");

        var role = user.Roles.FirstOrDefault(existingRole => existingRole.Name == roleName)
            ?? throw new NotFoundException($"Role '{roleName}' not found for user with ID {id}.");

        user.Roles.Remove(role);
        await _dbContext.SaveChangesAsync();
    }

    public async Task ChangePasswordAsync(Guid id, string currentPassword, string newPassword)
    {
        var user = await _dbContext.AuthUsers.FirstOrDefaultAsync(authUser => authUser.Id == id)
            ?? throw new NotFoundException($"User with ID {id} not found.");

        if (user.PasswordHash == null || user.Salt == null || !PasswordHelper.VerifyPassword(currentPassword, user.PasswordHash, user.Salt))
            throw new InvalidCredentialsException("Current password is incorrect.");

        PasswordHelper.CreatePasswordHash(newPassword, out var hash, out var salt);
        user.PasswordHash = hash;
        user.Salt = salt;

        await _dbContext.SaveChangesAsync();
    }

    public async Task SeedInitialUserAsync(IConfiguration configuration)
    {
        if (await _dbContext.AuthUsers.AnyAsync())
            return;

        var initialUserSection = configuration.GetSection("InitialUser");
        var username = initialUserSection["Username"];
        var password = initialUserSection["Password"];
        var roleName = initialUserSection["Role"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(roleName))
            return;

        var normalizedUsername = username.Normalize();
        PasswordHelper.CreatePasswordHash(password, out var hash, out var salt);

        var role = await _dbContext.Roles.FirstOrDefaultAsync(existingRole => existingRole.Name == roleName);
        if (role == null)
        {
            role = new Role { Name = roleName };
            _dbContext.Roles.Add(role);
            await _dbContext.SaveChangesAsync();
        }

        var user = new AuthUser
        {
            Username = normalizedUsername,
            PasswordHash = hash,
            Salt = salt,
            Roles = [role]
        };

        _dbContext.AuthUsers.Add(user);
        await _dbContext.SaveChangesAsync();
    }
}
