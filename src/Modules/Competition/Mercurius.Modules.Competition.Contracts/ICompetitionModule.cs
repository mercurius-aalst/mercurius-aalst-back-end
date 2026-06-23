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
}
