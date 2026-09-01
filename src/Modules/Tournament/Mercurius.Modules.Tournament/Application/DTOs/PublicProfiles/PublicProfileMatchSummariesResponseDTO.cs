namespace Mercurius.Modules.Tournament.Application.DTOs.PublicProfiles;

internal sealed class PublicProfileMatchSummariesResponseDTO
{
    public IReadOnlyList<PublicProfileMatchSummaryResponseDTO> PreviousMatches { get; init; } = [];
    public IReadOnlyList<PublicProfileMatchSummaryResponseDTO> UpcomingMatches { get; init; } = [];
}
