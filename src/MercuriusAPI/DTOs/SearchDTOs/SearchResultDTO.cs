using System.Text.Json.Serialization;

namespace Mercurius.LAN.API.DTOs.SearchDTOs;

public sealed class SearchResultDTO
{
    public required string Type { get; init; }
    public required string DisplayLabel { get; init; }
    public required string SupportingText { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Username { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TeamName { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? GameId { get; init; }
}
