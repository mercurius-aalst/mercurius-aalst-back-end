using Mercurius.Modules.Teams.Contracts;

namespace Mercurius.Modules.Teams.Services;

internal sealed class NullTeamCompetitionReadService : ITeamCompetitionReadService
{
    public Task<IReadOnlyList<PublicTeamTournamentSummary>> GetPublicTeamTournamentsAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<PublicTeamTournamentSummary>>([]);
    }

    public Task<bool> IsUserInProtectedTournamentRosterAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    public Task<bool> IsTeamInDeleteBlockingTournamentAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }
}
