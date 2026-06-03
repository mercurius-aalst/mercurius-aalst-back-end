namespace Mercurius.LAN.API.DTOs.SearchDTOs;

public sealed class SearchResponseDTO
{
    public IReadOnlyList<SearchResultDTO> Results { get; init; } = [];

    public string? NextCursor { get; init; }

    public bool HasMore { get; init; }
}
