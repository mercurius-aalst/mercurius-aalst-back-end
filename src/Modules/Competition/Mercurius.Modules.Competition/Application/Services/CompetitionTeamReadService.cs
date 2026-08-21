using Mercurius.Modules.Competition.Domain;
using Mercurius.Modules.Competition.Infrastructure;
using Mercurius.Modules.Shared;
using Mercurius.Modules.Teams.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.Modules.Competition.Application.Services;

internal sealed class CompetitionTeamReadService : ITeamCompetitionReadService
{
    private readonly ICompetitionDbContext _dbContext;

    public CompetitionTeamReadService(ICompetitionDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<PublicTeamTournamentSummary>> GetPublicTeamTournamentsAsync(
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.TournamentRegistrations
            .AsNoTracking()
            .Where(registration =>
                registration.TeamId == teamId &&
                registration.Status == TournamentRegistrationStatus.Active &&
                registration.Game.Status != GameStatus.Canceled)
            .OrderBy(registration => registration.Game.Name)
            .ThenBy(registration => registration.GameId)
            .Select(registration => new PublicTeamTournamentSummary(
                new GameId(registration.GameId),
                registration.Game.Name))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> IsUserInProtectedTournamentRosterAsync(
        Guid teamId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TournamentRegistrationRosterMembers.AnyAsync(
            member =>
                member.TeamId == teamId &&
                member.UserId == userId &&
                (member.Game.Status == GameStatus.Scheduled ||
                 member.Game.Status == GameStatus.InProgress),
            cancellationToken);
    }

    public Task<bool> IsTeamInDeleteBlockingTournamentAsync(
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TournamentRegistrations.AnyAsync(
            registration =>
                registration.TeamId == teamId &&
                (registration.Game.Status == GameStatus.Scheduled ||
                 registration.Game.Status == GameStatus.InProgress),
            cancellationToken);
    }

    public Task<bool> IsTeamLogoReferencedAsync(
        string logoUrl,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TournamentRegistrations
            .AsNoTracking()
            .AnyAsync(
                registration => registration.TeamLogoUrlAtRegistration == logoUrl,
                cancellationToken);
    }
}
