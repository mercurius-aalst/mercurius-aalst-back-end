using Mercurius.LAN.API.Data;
using Mercurius.Modules.Shared;
using Mercurius.Modules.Teams.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.LAN.API.Services.TeamServices;

public sealed class EfTeamRealtimeAuthorizer : ITeamRealtimeAuthorizer
{
    private readonly MercuriusDBContext _dbContext;

    public EfTeamRealtimeAuthorizer(MercuriusDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> CanSubscribeToTeamAsync(
        TeamId teamId,
        UserId userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Teams
            .AsNoTracking()
            .AnyAsync(team =>
                !team.IsDeleted &&
                team.Id == teamId.Value &&
                (team.CaptainUserId == userId.Value || team.Members.Any(member => member.Id == userId.Value)),
                cancellationToken);
    }
}
