using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.DTOs.Auth;
using Mercurius.LAN.API.Exceptions;
using Microsoft.EntityFrameworkCore;
using Mercurius.LAN.API.Models;
using Mercurius.LAN.API.Extensions;

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
        return await _dbContext.Users.Select(u => new GetUserDTO(u)).ToListAsync();
    }

    public async Task<GetUserDTO> CreateUserAsync(CreateUserProfileRequest request)
    {
        return await CreateUserAsync(
            request.Auth0Subject,
            request.Username,
            request.Firstname,
            request.Lastname,
            request.Email,
            request.DiscordId,
            request.SteamId,
            request.RiotId);
    }

    public async Task<GetUserDTO> CompleteProfileAsync(string auth0Subject, CompleteUserProfileRequest request)
    {
        if (await _dbContext.Users.AnyAsync(u => u.Auth0Subject == auth0Subject.Trim()))
            throw new ValidationException("Profile already completed.");

        return await CreateUserAsync(
            auth0Subject,
            request.Username,
            request.Firstname,
            request.Lastname,
            request.Email,
            request.DiscordId,
            request.SteamId,
            request.RiotId);
    }

    public async Task<CurrentUserProfileResponse> GetCurrentUserAsync(string auth0Subject)
    {
        var subject = NormalizeAuth0Subject(auth0Subject);
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Auth0Subject == subject);
        return user == null
            ? new CurrentUserProfileResponse(false, null)
            : new CurrentUserProfileResponse(true, new GetUserDTO(user));
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

        var normalizedUsername = request.Username.Normalize();
        var normalizedEmail = NormalizeEmail(request.Email);

        if (!string.Equals(user.Username, normalizedUsername, StringComparison.Ordinal) &&
            await _dbContext.Users.AnyAsync(u => u.Username == normalizedUsername && u.Id != id))
        {
            throw new ValidationException("Username already exists");
        }

        if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase) &&
            await _dbContext.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail && u.Id != id))
        {
            throw new ValidationException("Email already exists");
        }

        user.Username = normalizedUsername;
        user.UpdateProfile(request.Firstname.Trim(), request.Lastname.Trim(), request.Email.Trim(), request.DiscordId, request.SteamId, request.RiotId);

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

    private async Task<GetUserDTO> CreateUserAsync(
        string auth0Subject,
        string username,
        string firstname,
        string lastname,
        string email,
        string? discordId,
        string? steamId,
        string? riotId)
    {
        var normalizedAuth0Subject = NormalizeAuth0Subject(auth0Subject);
        var normalizedUsername = username.Normalize();
        var normalizedEmail = NormalizeEmail(email);

        if (await _dbContext.Users.AnyAsync(u => u.Auth0Subject == normalizedAuth0Subject))
            throw new ValidationException("Auth0 subject already exists");

        if (await _dbContext.Users.AnyAsync(u => u.Username == normalizedUsername))
            throw new ValidationException("Username already exists");

        if (await _dbContext.Users.AnyAsync(u => u.Email.ToLower() == normalizedEmail))
            throw new ValidationException("Email already exists");

        var user = new User
        {
            Auth0Subject = normalizedAuth0Subject,
            Username = normalizedUsername
        };

        user.UpdateProfile(firstname.Trim(), lastname.Trim(), email.Trim(), discordId, steamId, riotId);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        return new GetUserDTO(user);
    }

    private static string NormalizeAuth0Subject(string auth0Subject)
    {
        if (string.IsNullOrWhiteSpace(auth0Subject))
            throw new ValidationException("Auth0 subject is required.");

        return auth0Subject.Trim();
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
