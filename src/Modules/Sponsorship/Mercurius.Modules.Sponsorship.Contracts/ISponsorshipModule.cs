using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Sponsorship.Contracts;

public interface ISponsorshipModule
{
    Task<SponsorSummary?> GetSponsorSummaryAsync(
        SponsorId sponsorId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SponsorSummary>> GetSponsorsAsync(
        CancellationToken cancellationToken = default);

    Task<SponsorPlacementSummary?> GetSponsorPlacementAsync(
        GameId gameId,
        CancellationToken cancellationToken = default);
}
