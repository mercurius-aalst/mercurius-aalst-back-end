namespace Auth.Module.Persistence;

public interface IExternalUserProfileProvisioner
{
    Task<Guid?> FindUserIdByVerifiedEmailAsync(string email, CancellationToken cancellationToken = default);
    Task CreateMinimalProfileAsync(Guid userId, string username, string email, string? firstName, string? lastName, CancellationToken cancellationToken = default);
}
