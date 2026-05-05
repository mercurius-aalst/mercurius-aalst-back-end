using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.DTOs.UserDTOs;

public class GetUserDTO
{
    public Guid Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Firstname { get; init; } = string.Empty;
    public string Lastname { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? DiscordId { get; init; } = string.Empty;
    public string? SteamId { get; init; } = string.Empty;
    public string? RiotId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public IEnumerable<string> Roles { get; init; } = [];

    public GetUserDTO(User user)
        : this(user, [])
    {
    }

    public GetUserDTO(User user, IEnumerable<string> roles)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(roles);

        Id = user.Id;
        Username = user.Username;
        Firstname = user.Firstname;
        Lastname = user.Lastname;
        Email = user.Email;
        DiscordId = user.DiscordId;
        SteamId = user.SteamId;
        RiotId = user.RiotId;
        DisplayName = user.DisplayName;
        Roles = roles.ToArray();
    }
}
