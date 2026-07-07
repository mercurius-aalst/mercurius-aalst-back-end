using Mercurius.Modules.Teams.DTOs;

namespace Mercurius.Modules.Teams.Services;

public interface ITeamCompetitionReadService
{
    Task<IReadOnlyList<PublicTeamTournamentDTO>> GetPublicTeamTournamentsAsync(Guid teamId, CancellationToken cancellationToken = default);

    Task<bool> IsUserInProtectedTournamentRosterAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default);

    Task<bool> IsTeamInDeleteBlockingTournamentAsync(Guid teamId, CancellationToken cancellationToken = default);
}

internal sealed class NullTeamCompetitionReadService : ITeamCompetitionReadService
{
    public Task<IReadOnlyList<PublicTeamTournamentDTO>> GetPublicTeamTournamentsAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<PublicTeamTournamentDTO>>([]);
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
