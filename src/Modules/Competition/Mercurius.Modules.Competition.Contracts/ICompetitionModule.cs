using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Competition.Contracts;

public interface ICompetitionModule
{
    Task<GameSummary?> GetGameSummaryAsync(
        GameId gameId,
        CancellationToken cancellationToken = default);

    Task<TournamentConfiguration?> GetTournamentConfigurationAsync(
        GameId gameId,
        CancellationToken cancellationToken = default);

    Task<bool> IsRegistrationOpenAsync(
        GameId gameId,
        CancellationToken cancellationToken = default);

    Task<RegistrationEligibility> CheckIndividualRegistrationEligibilityAsync(
        GameId gameId,
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<RegistrationEligibility> CheckTeamRegistrationEligibilityAsync(
        GameId gameId,
        TeamId teamId,
        UserId requestedBy,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GameSummary>> SearchGamesAsync(
        string normalizedQuery,
        CompetitionSearchCursor? cursor,
        int limit,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GameSearchDocument>> GetGameSearchDocumentsAsync(
        CancellationToken cancellationToken = default);
}
