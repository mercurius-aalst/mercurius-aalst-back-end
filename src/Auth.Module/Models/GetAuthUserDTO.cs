namespace Auth.Module.Models;

public class GetAuthUserDTO
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public IEnumerable<string> Roles { get; init; } = [];

    public GetAuthUserDTO(AuthUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        Id = user.Id;
        Username = user.Username;
        Roles = user.Roles.Select(role => role.Name).ToArray();
    }
}
