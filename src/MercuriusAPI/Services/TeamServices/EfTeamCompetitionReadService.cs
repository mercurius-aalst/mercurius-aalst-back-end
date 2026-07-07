using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.Models;
using Mercurius.Modules.Teams.DTOs;
using Mercurius.Modules.Teams.Services;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.LAN.API.Services.TeamServices;

public sealed class EfTeamCompetitionReadService : ITeamCompetitionReadService
{
    private readonly MercuriusDBContext _dbContext;

    public EfTeamCompetitionReadService(MercuriusDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PublicTeamTournamentDTO>> GetPublicTeamTournamentsAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TournamentRegistrations
            .AsNoTracking()
            .Where(registration =>
                registration.TeamId == teamId &&
                registration.Status == TournamentRegistrationStatus.Active &&
                registration.Game.Status != GameStatus.Canceled)
            .Select(registration => new PublicTeamTournamentDTO
            {
                GameId = registration.GameId,
                Name = registration.Game.Name
            })
            .OrderBy(tournament => tournament.Name)
            .ThenBy(tournament => tournament.GameId)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> IsUserInProtectedTournamentRosterAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.TournamentRegistrationRosterMembers.AnyAsync(member =>
            member.TeamId == teamId &&
            member.UserId == userId &&
            (member.Game.Status == GameStatus.Scheduled || member.Game.Status == GameStatus.InProgress),
            cancellationToken);
    }

    public Task<bool> IsTeamInDeleteBlockingTournamentAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        return _dbContext.TournamentRegistrations.AnyAsync(registration =>
            registration.TeamId == teamId &&
            (registration.Game.Status == GameStatus.Scheduled || registration.Game.Status == GameStatus.InProgress),
            cancellationToken);
    }
}
