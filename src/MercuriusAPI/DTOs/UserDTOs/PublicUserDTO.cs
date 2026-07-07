using Mercurius.Modules.Identity.Contracts;
using Mercurius.Modules.Identity.Domain;

namespace Mercurius.LAN.API.DTOs.UserDTOs;

public class PublicUserDTO
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? DiscordId { get; set; }
    public string? SteamId { get; set; }
    public string? RiotId { get; set; }

    public PublicUserDTO()
    {
    }

    public PublicUserDTO(User user)
    {
        Id = user.Id;
        Username = string.IsNullOrWhiteSpace(user.Username) ? "Incomplete profile" : user.Username;
        DisplayName = Username;
        DiscordId = user.DiscordId;
        SteamId = user.SteamId;
        RiotId = user.RiotId;
    }

    public PublicUserDTO(UserProfileSummary user)
    {
        Id = user.Id.Value;
        Username = string.IsNullOrWhiteSpace(user.Username) ? "Incomplete profile" : user.Username;
        DisplayName = Username;
        DiscordId = user.DiscordId;
        SteamId = user.SteamId;
        RiotId = user.RiotId;
    }
}
