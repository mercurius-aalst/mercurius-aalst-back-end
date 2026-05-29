using System.Text.Json.Serialization;
using Mercurius.LAN.API.DTOs.TeamDTOs;
using Mercurius.LAN.API.Models;

namespace Mercurius.LAN.API.DTOs.Auth;

public class GetPublicUserDTO
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string DisplayName { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DiscordId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SteamId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RiotId { get; set; }

    public GetPublicUserDTO()
    {
    }

    public GetPublicUserDTO(User user, bool includePlatformIds = false)
    {
        Id = user.Id;
        DisplayName = user.DisplayName;
        Username = NormalizeUsername(user.Username, user.DisplayName);
        ApplyPlatformIds(includePlatformIds, user.DiscordId, user.SteamId, user.RiotId);
    }

    public GetPublicUserDTO(GetUserDTO user, bool includePlatformIds = false)
    {
        Id = user.Id;
        DisplayName = user.DisplayName;
        Username = NormalizeUsername(user.Username, user.DisplayName);
        ApplyPlatformIds(includePlatformIds, user.DiscordId, user.SteamId, user.RiotId);
    }

    public GetPublicUserDTO(GetTeamUserDTO user, bool includePlatformIds = false)
    {
        Id = user.Id;
        DisplayName = user.DisplayName;
        Username = NormalizeUsername(user.Username, user.DisplayName);
        ApplyPlatformIds(includePlatformIds, user.DiscordId, user.SteamId, user.RiotId);
    }

    private void ApplyPlatformIds(bool includePlatformIds, string? discordId, string? steamId, string? riotId)
    {
        if (!includePlatformIds)
            return;

        DiscordId = string.IsNullOrWhiteSpace(discordId) ? null : discordId;
        SteamId = string.IsNullOrWhiteSpace(steamId) ? null : steamId;
        RiotId = string.IsNullOrWhiteSpace(riotId) ? null : riotId;
    }

    private static string NormalizeUsername(string? username, string displayName)
    {
        return string.IsNullOrWhiteSpace(username) ? displayName : username;
    }
}
