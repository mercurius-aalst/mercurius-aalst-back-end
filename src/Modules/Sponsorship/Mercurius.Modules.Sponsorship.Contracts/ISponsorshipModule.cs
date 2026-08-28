using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Sponsorship.Contracts;

public interface ISponsorshipModule
{
    Task<SponsorSummary?> GetSponsorSummaryAsync(
        SponsorId sponsorId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SponsorSearchDocument>> GetSponsorSearchDocumentsPageAsync(
        SponsorId? afterId,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<TournamentId, SponsorPlacementSummary>> GetSponsorPlacementsAsync(
        IReadOnlyCollection<TournamentId> tournamentIds,
        CancellationToken cancellationToken = default);

    Task<SponsorPlacementSummary?> GetSponsorPlacementAsync(
        TournamentId tournamentId,
        CancellationToken cancellationToken = default);

    Task ReplaceSponsorPlacementAsync(
        TournamentId tournamentId,
        SponsorPlacementInput? placement,
        CancellationToken cancellationToken = default);
}
