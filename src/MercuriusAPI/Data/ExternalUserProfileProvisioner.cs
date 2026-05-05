using Auth.Module.Persistence;
using Mercurius.LAN.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.LAN.API.Data;

public class ExternalUserProfileProvisioner : IExternalUserProfileProvisioner
{
    private readonly MercuriusDBContext _dbContext;

    public ExternalUserProfileProvisioner(MercuriusDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Guid?> FindUserIdByVerifiedEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        return _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Email == email)
            .Select(user => (Guid?)user.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task CreateMinimalProfileAsync(Guid userId, string username, string email, string? firstName, string? lastName, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = username.Normalize();
        var fallbackName = normalizedUsername;

        var user = new User
        {
            Id = userId,
            Username = normalizedUsername,
            Firstname = string.IsNullOrWhiteSpace(firstName) ? fallbackName : firstName,
            Lastname = string.IsNullOrWhiteSpace(lastName) ? fallbackName : lastName,
            Email = email
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
