using System.Text.Json.Serialization;

namespace Mercurius.LAN.API.DTOs.SearchDTOs;

public sealed class SearchResponseDTO
{
    public IReadOnlyList<SearchResultDTO> Results { get; init; } = [];

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NextCursor { get; init; }

    public int TotalCount { get; init; }
    public bool HasMore { get; init; }
}
