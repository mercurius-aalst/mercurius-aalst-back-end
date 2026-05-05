using Mercurius.LAN.API.DTOs.UserDTOs;
using Mercurius.LAN.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.LAN.API.Data;

public class UserProfileStore
{
    private readonly MercuriusDBContext _dbContext;

    public UserProfileStore(MercuriusDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GetUserDTO?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        return user == null ? null : await CreateUserDtoAsync(user, cancellationToken);
    }

    public async Task<GetUserDTO?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

        return user == null ? null : await CreateUserDtoAsync(user, cancellationToken);
    }

    public Task<bool> EmailExistsAsync(string email, Guid? excludedUserId = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        return _dbContext.Users.AnyAsync(
            u => u.Email == email && (!excludedUserId.HasValue || u.Id != excludedUserId.Value),
            cancellationToken);
    }

    public async Task<IEnumerable<GetUserDTO>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _dbContext.Users
            .AsNoTracking()
            .OrderBy(u => u.Username)
            .ToListAsync(cancellationToken);

        var roleLookup = await _dbContext.AuthUsers
            .AsNoTracking()
            .Include(u => u.Roles)
            .ToDictionaryAsync(u => u.Id, u => (IEnumerable<string>)u.Roles.Select(role => role.Name).ToArray(), cancellationToken);

        return users.Select(user => MapUserDto(user, roleLookup.GetValueOrDefault(user.Id, []))).ToList();
    }

    public async Task CreateAsync(CreateUserProfileRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = new User
        {
            Id = userId,
            Username = request.Username.Normalize(),
            Firstname = request.Firstname,
            Lastname = request.Lastname,
            Email = request.Email,
            DiscordId = request.DiscordId,
            SteamId = request.SteamId,
            RiotId = request.RiotId
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Guid id, UpdateUserProfileRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user == null)
            return;

        user.Username = request.Username.Normalize();
        user.UpdateProfile(request.Firstname, request.Lastname, request.Email, request.DiscordId, request.SteamId, request.RiotId);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        if (user == null)
            return;

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<GetUserDTO> CreateUserDtoAsync(User user, CancellationToken cancellationToken)
    {
        var roles = await _dbContext.AuthUsers
            .AsNoTracking()
            .Where(authUser => authUser.Id == user.Id)
            .SelectMany(authUser => authUser.Roles.Select(role => role.Name))
            .ToListAsync(cancellationToken);

        return MapUserDto(user, roles);
    }

    private static GetUserDTO MapUserDto(User user, IEnumerable<string> roles)
    {
        return new GetUserDTO(user, roles);
    }
}
