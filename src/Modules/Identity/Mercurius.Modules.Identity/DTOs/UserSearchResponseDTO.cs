namespace Mercurius.Modules.Identity.DTOs;

public sealed class UserSearchResponseDTO
{
    public IReadOnlyList<UserSearchResultDTO> Results { get; init; } = [];

    public string? NextCursor { get; init; }

    public bool HasMore { get; init; }
}
