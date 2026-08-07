using Mercurius.Modules.Competition.Contracts;
using Mercurius.Modules.Discovery.Contracts;
using Mercurius.Modules.Discovery.Domain;
using Mercurius.Modules.Discovery.Infrastructure;
using Mercurius.Modules.Identity.Contracts;
using Mercurius.Modules.Sponsorship.Contracts;
using Mercurius.Modules.Teams.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mercurius.Modules.Discovery.Application;

internal sealed class SearchIndexRebuildService
{
    private const string FailureMessage = "The rebuild failed. Check server logs for details.";
    private readonly IDiscoveryDbContext _dbContext;
    private readonly SearchDocumentProjector _projector;
    private readonly IIdentityModule _identityModule;
    private readonly ITeamsModule _teamsModule;
    private readonly ICompetitionModule _competitionModule;
    private readonly ISponsorshipModule _sponsorshipModule;
    private readonly ILogger<SearchIndexRebuildService> _logger;

    public SearchIndexRebuildService(
        IDiscoveryDbContext dbContext,
        SearchDocumentProjector projector,
        IIdentityModule identityModule,
        ITeamsModule teamsModule,
        ICompetitionModule competitionModule,
        ISponsorshipModule sponsorshipModule,
        ILogger<SearchIndexRebuildService> logger)
    {
        _dbContext = dbContext;
        _projector = projector;
        _identityModule = identityModule;
        _teamsModule = teamsModule;
        _competitionModule = competitionModule;
        _sponsorshipModule = sponsorshipModule;
        _logger = logger;
    }

    public async Task<DiscoverySearchIndexRebuildJob> CreateJobAsync(CancellationToken cancellationToken)
    {
        var activeJob = await _dbContext.SearchIndexRebuildJobs
            .AsNoTracking()
            .Where(job => job.Status == SearchIndexRebuildJobStatus.Pending || job.Status == SearchIndexRebuildJobStatus.Running)
            .OrderBy(job => job.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (activeJob is not null)
            return ToContract(activeJob);

        var job = new SearchIndexRebuildJob
        {
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.SearchIndexRebuildJobs.Add(job);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return ToContract(job);
        }
        catch (DbUpdateException)
        {
            _dbContext.Entry(job).State = EntityState.Detached;
            var existingJob = await _dbContext.SearchIndexRebuildJobs
                .AsNoTracking()
                .Where(candidate => candidate.Status == SearchIndexRebuildJobStatus.Pending || candidate.Status == SearchIndexRebuildJobStatus.Running)
                .OrderBy(candidate => candidate.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);
            if (existingJob is not null)
                return ToContract(existingJob);

            throw;
        }
    }

    public async Task EnsureInitialJobAsync(CancellationToken cancellationToken)
    {
        var hasProjectedDocuments = await _dbContext.SearchDocuments
            .AsNoTracking()
            .AnyAsync(cancellationToken);
        if (hasProjectedDocuments)
            return;

        var hasCompletedJob = await _dbContext.SearchIndexRebuildJobs
            .AsNoTracking()
            .AnyAsync(job => job.Status == SearchIndexRebuildJobStatus.Completed, cancellationToken);
        if (hasCompletedJob)
            return;

        _ = await CreateJobAsync(cancellationToken);
    }

    public async Task<DiscoverySearchIndexRebuildJob?> GetJobAsync(
        Guid jobId,
        CancellationToken cancellationToken)
    {
        var job = await _dbContext.SearchIndexRebuildJobs
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == jobId, cancellationToken);
        return job is null ? null : ToContract(job);
    }

    public async Task<bool> RunNextAsync(CancellationToken cancellationToken)
    {
        var job = await _dbContext.SearchIndexRebuildJobs
            .Where(candidate => candidate.Status == SearchIndexRebuildJobStatus.Pending)
            .OrderBy(candidate => candidate.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (job is null)
            return false;

        job.Status = SearchIndexRebuildJobStatus.Running;
        job.StartedAtUtc = DateTime.UtcNow;
        job.Error = null;
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var sourceVersion = job.StartedAtUtc.Value.Ticks;
            await RebuildDocumentsAsync(sourceVersion, job.StartedAtUtc.Value, cancellationToken);

            job.Status = SearchIndexRebuildJobStatus.Completed;
            job.CompletedAtUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Discovery search-index rebuild job {JobId} failed.", job.Id);
            job.Status = SearchIndexRebuildJobStatus.Failed;
            job.CompletedAtUtc = DateTime.UtcNow;
            job.Error = FailureMessage;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return true;
    }

    private async Task RebuildDocumentsAsync(
        long sourceVersion,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken)
    {
        var users = await _identityModule.GetPublicUserSearchDocumentsAsync(cancellationToken);
        await _projector.RebuildAsync(
            SearchDocumentTypes.User,
            users.Select(user => new SearchDocumentProjection(
                user.UserId.Value.ToString(),
                user.Username,
                "User",
                ImageUrl: null,
                $"/users/{Uri.EscapeDataString(user.Username)}")).ToArray(),
            sourceVersion,
            updatedAtUtc,
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var teams = await _teamsModule.GetPublicTeamSearchDocumentsAsync(cancellationToken);
        await _projector.RebuildAsync(
            SearchDocumentTypes.Team,
            teams.Select(team => new SearchDocumentProjection(
                team.TeamId.Value.ToString(),
                team.Name,
                "Team",
                ImageUrl: null,
                $"/teams/{Uri.EscapeDataString(team.Name)}")).ToArray(),
            sourceVersion,
            updatedAtUtc,
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var games = await _competitionModule.GetGameSearchDocumentsAsync(cancellationToken);
        await _projector.RebuildAsync(
            SearchDocumentTypes.Game,
            games.Select(game => new SearchDocumentProjection(
                game.GameId.Value.ToString(),
                game.Name,
                "Game",
                game.ImageUrl,
                $"/games/{game.GameId.Value}")).ToArray(),
            sourceVersion,
            updatedAtUtc,
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var sponsors = await _sponsorshipModule.GetSponsorsAsync(cancellationToken);
        await _projector.RebuildAsync(
            SearchDocumentTypes.Sponsor,
            sponsors.Select(sponsor => new SearchDocumentProjection(
                sponsor.Id.Value.ToString(),
                sponsor.Name,
                "Sponsor",
                sponsor.LogoUrl,
                $"/sponsors/{sponsor.Id.Value}")).ToArray(),
            sourceVersion,
            updatedAtUtc,
            cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static DiscoverySearchIndexRebuildJob ToContract(SearchIndexRebuildJob job)
    {
        return new DiscoverySearchIndexRebuildJob(
            job.Id,
            job.Status.ToString().ToLowerInvariant(),
            job.CreatedAtUtc,
            job.StartedAtUtc,
            job.CompletedAtUtc,
            job.Error);
    }
}
