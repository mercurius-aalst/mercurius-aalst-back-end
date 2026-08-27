using Mercurius.Modules.Tournament.Domain;
using Mercurius.Modules.Tournament.Infrastructure;
using Mercurius.Modules.Shared;
using Mercurius.Modules.Teams.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.Modules.Tournament.Application.Services;

internal sealed class TournamentTeamReadService : ITeamTournamentReadService
{
    private readonly ITournamentDbContext _dbContext;

    public TournamentTeamReadService(ITournamentDbContext dbContext)
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
                registration.Tournament.Status != TournamentStatus.Canceled)
            .OrderBy(registration => registration.Tournament.Name)
            .ThenBy(registration => registration.TournamentId)
            .Select(registration => new PublicTeamTournamentSummary(
                new TournamentId(registration.TournamentId),
                registration.Tournament.Name))
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
                (member.Tournament.Status == TournamentStatus.Scheduled ||
                 member.Tournament.Status == TournamentStatus.InProgress),
            cancellationToken);
    }

    public Task<bool> IsTeamInDeleteBlockingTournamentAsync(
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.TournamentRegistrations.AnyAsync(
            registration =>
                registration.TeamId == teamId &&
                (registration.Tournament.Status == TournamentStatus.Scheduled ||
                 registration.Tournament.Status == TournamentStatus.InProgress),
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
