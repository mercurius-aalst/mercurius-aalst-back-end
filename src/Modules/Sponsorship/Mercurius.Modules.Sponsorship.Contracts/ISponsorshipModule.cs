using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Sponsorship.Contracts;

public interface ISponsorshipModule
{
    Task<SponsorSummary?> GetSponsorSummaryAsync(
        SponsorId sponsorId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SponsorSummary>> GetSponsorsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<GameId, SponsorPlacementSummary>> GetSponsorPlacementsAsync(
        IReadOnlyCollection<GameId> gameIds,
        CancellationToken cancellationToken = default);

    Task<SponsorPlacementSummary?> GetSponsorPlacementAsync(
        GameId gameId,
        CancellationToken cancellationToken = default);

    Task ReplaceSponsorPlacementAsync(
        GameId gameId,
        SponsorPlacementInput? placement,
        CancellationToken cancellationToken = default);
}
