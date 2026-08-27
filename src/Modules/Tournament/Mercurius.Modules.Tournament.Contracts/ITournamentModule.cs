using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Tournament.Contracts;

public interface ITournamentModule
{
    Task<TournamentSummary?> GetTournamentSummaryAsync(
        TournamentId tournamentId,
        CancellationToken cancellationToken = default);

    Task<TournamentConfiguration?> GetTournamentConfigurationAsync(
        TournamentId tournamentId,
        CancellationToken cancellationToken = default);

    Task<bool> IsRegistrationOpenAsync(
        TournamentId tournamentId,
        CancellationToken cancellationToken = default);

    Task<RegistrationEligibility> CheckIndividualRegistrationEligibilityAsync(
        TournamentId tournamentId,
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<RegistrationEligibility> CheckTeamRegistrationEligibilityAsync(
        TournamentId tournamentId,
        TeamId teamId,
        UserId requestedBy,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TournamentSummary>> SearchTournamentsAsync(
        string normalizedQuery,
        TournamentSearchCursor? cursor,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TournamentSearchDocument>> GetTournamentSearchDocumentsPageAsync(
        TournamentId? afterId,
        int pageSize,
        CancellationToken cancellationToken = default);
}
