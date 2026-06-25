using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Teams.Contracts;

public interface ITeamRealtimeAuthorizer
{
    Task<bool> CanSubscribeToTeamAsync(
        TeamId teamId,
        UserId userId,
        CancellationToken cancellationToken = default);
}
