using Mercurius.Modules.Teams.DTOs;
using Mercurius.Modules.Teams.Contracts;
using Mercurius.Modules.Teams.Domain;
using Mercurius.Modules.Shared;
using Mercurius.Modules.Teams.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Platform.Eventing;
using System.Runtime.ExceptionServices;
using TeamCaptainTransferredIntegrationEvent = Mercurius.Modules.Teams.Contracts.TeamCaptainTransferredIntegrationEvent;
using TeamCreatedIntegrationEvent = Mercurius.Modules.Teams.Contracts.TeamCreatedIntegrationEvent;
using TeamDeletedIntegrationEvent = Mercurius.Modules.Teams.Contracts.TeamDeletedIntegrationEvent;
using TeamMemberAddedIntegrationEvent = Mercurius.Modules.Teams.Contracts.TeamMemberAddedIntegrationEvent;
using TeamMemberRemovedIntegrationEvent = Mercurius.Modules.Teams.Contracts.TeamMemberRemovedIntegrationEvent;
using TeamRenamedIntegrationEvent = Mercurius.Modules.Teams.Contracts.TeamRenamedIntegrationEvent;

namespace Mercurius.Modules.Teams.Services;

internal sealed class TeamEventPublishingDecorator : ITeamService
{
    private readonly ITeamService _inner;
    private readonly ITeamsDbContext _dbContext;
    private readonly ITeamEventPublisher _teamEventPublisher;
    private readonly IModuleEventPublisher? _moduleEventPublisher;
    private DbSet<TeamInvite> TeamInvites => _dbContext.Set<TeamInvite>();

    public TeamEventPublishingDecorator(
        ITeamService inner,
        ITeamsDbContext dbContext,
        ITeamEventPublisher? teamEventPublisher = null,
        IModuleEventPublisher? moduleEventPublisher = null)
    {
        _inner = inner;
        _dbContext = dbContext;
        _teamEventPublisher = teamEventPublisher ?? new NullTeamEventPublisher();
        _moduleEventPublisher = moduleEventPublisher;
    }

    public async Task<GetTeamDTO> CreateTeamAsync(CreateTeamDTO teamDTO, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithDurableEventTransactionAsync(
            () => _inner.CreateTeamAsync(teamDTO, cancellationToken),
            team => PublishTeamCreatedAsync(team.Id, team.CaptainUserId, cancellationToken),
            cancellationToken);
    }

    public async Task<TeamManagementSummaryDTO> CreateCurrentUserTeamAsync(string auth0UserId, CreateTeamDTO teamDTO, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithDurableEventTransactionAsync(
            () => _inner.CreateCurrentUserTeamAsync(auth0UserId, teamDTO, cancellationToken),
            team => PublishTeamCreatedAsync(team.Id, team.CaptainUserId, cancellationToken),
            cancellationToken);
    }

    public async Task DeleteTeamAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        await ExecuteWithDurableEventTransactionAsync(
            async () =>
            {
                var shouldPublishDeleted = await _dbContext.Teams
                    .AsNoTracking()
                    .AnyAsync(team => team.Id == teamId && !team.IsDeleted, cancellationToken);

                await _inner.DeleteTeamAsync(teamId, cancellationToken);
                return shouldPublishDeleted;
            },
            shouldPublishDeleted => shouldPublishDeleted
                ? PublishTeamDeletedAsync(teamId, cancellationToken)
                : Task.CompletedTask,
            cancellationToken);
    }

    public async Task DeleteTeamAsync(string auth0UserId, Guid teamId, CancellationToken cancellationToken = default)
    {
        await ExecuteWithDurableEventTransactionAsync(
            () => _inner.DeleteTeamAsync(auth0UserId, teamId, cancellationToken),
            () => PublishTeamDeletedAsync(teamId, cancellationToken),
            cancellationToken);
    }

    public Task<IEnumerable<GetTeamDTO>> GetAllTeamsAsync(CancellationToken cancellationToken = default)
    {
        return _inner.GetAllTeamsAsync(cancellationToken);
    }

    public async Task<CurrentUserTeamSummaryDTO> GetCurrentUserTeamSummaryAsync(string auth0UserId, CancellationToken cancellationToken = default)
    {
        return await PublishExpiredInviteEventsAroundAsync(
            () => _inner.GetCurrentUserTeamSummaryAsync(auth0UserId, cancellationToken),
            cancellationToken: cancellationToken);
    }

    public Task<PublicTeamProfileDTO> GetPublicTeamProfileAsync(string teamName, CancellationToken cancellationToken = default)
    {
        return _inner.GetPublicTeamProfileAsync(teamName, cancellationToken);
    }

    public Task<IEnumerable<TeamInviteDTO>> GetUserInvitesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _inner.GetUserInvitesAsync(userId, cancellationToken);
    }

    public async Task<IEnumerable<TeamInviteSummaryDTO>> GetCurrentUserInvitesAsync(string auth0UserId, CancellationToken cancellationToken = default)
    {
        return await PublishExpiredInviteEventsAroundAsync(
            () => _inner.GetCurrentUserInvitesAsync(auth0UserId, cancellationToken),
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<TeamInviteSummaryDTO>> GetCurrentUserSentInvitesAsync(string auth0UserId, CancellationToken cancellationToken = default)
    {
        return await PublishExpiredInviteEventsAroundAsync(
            () => _inner.GetCurrentUserSentInvitesAsync(auth0UserId, cancellationToken),
            cancellationToken: cancellationToken);
    }

    public Task<GetTeamDTO> GetTeamByIdAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        return _inner.GetTeamByIdAsync(teamId, cancellationToken);
    }

    public Task<GetTeamDTO> GetTeamByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return _inner.GetTeamByNameAsync(name, cancellationToken);
    }

    public async Task<TeamInviteDTO> InviteUserAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await PublishExpiredInviteEventsAroundAsync(
            () => _inner.InviteUserAsync(teamId, userId, cancellationToken),
            teamId,
            userId,
            cancellationToken);
    }

    public async Task<TeamInviteDTO> InviteUserAsync(string auth0UserId, Guid teamId, Guid userId, CancellationToken cancellationToken = default)
    {
        var invite = await PublishExpiredInviteEventsAroundAsync(
            () => _inner.InviteUserAsync(auth0UserId, teamId, userId, cancellationToken),
            teamId,
            userId,
            cancellationToken);

        await _teamEventPublisher.InviteChangedAsync(teamId, invite.Id, userId, invite.Status, cancellationToken);
        return invite;
    }

    public async Task<TeamInviteDTO> CancelInviteAsync(string auth0UserId, Guid teamId, Guid inviteId, CancellationToken cancellationToken = default)
    {
        var invite = await _inner.CancelInviteAsync(auth0UserId, teamId, inviteId, cancellationToken);
        await _teamEventPublisher.InviteChangedAsync(teamId, invite.Id, invite.UserId, invite.Status, cancellationToken);
        return invite;
    }

    public async Task<GetTeamDTO> RemoveMemberAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithDurableEventTransactionAsync(
            () => _inner.RemoveMemberAsync(id, userId, cancellationToken),
            _ => PublishTeamMemberRemovedAsync(id, userId, cancellationToken),
            cancellationToken);
    }

    public async Task<TeamManagementSummaryDTO> RemoveMemberAsync(string auth0UserId, Guid teamId, Guid userId, CancellationToken cancellationToken = default)
    {
        var team = await ExecuteWithDurableEventTransactionAsync(
            () => _inner.RemoveMemberAsync(auth0UserId, teamId, userId, cancellationToken),
            _ => PublishTeamMemberRemovedAsync(teamId, userId, cancellationToken),
            cancellationToken);

        await _teamEventPublisher.MembershipChangedAsync(teamId, userId, "Removed", cancellationToken);
        return team;
    }

    public async Task<TeamManagementSummaryDTO> LeaveTeamAsync(string auth0UserId, Guid teamId, CancellationToken cancellationToken = default)
    {
        var result = await ExecuteWithDurableEventTransactionAsync(
            async () =>
            {
                var team = await _inner.LeaveTeamAsync(auth0UserId, teamId, cancellationToken);
                var userId = await GetCurrentUserIdAsync(auth0UserId, cancellationToken);
                return new TeamMemberRemovedResult(team, userId);
            },
            result => PublishTeamMemberRemovedAsync(teamId, result.UserId, cancellationToken),
            cancellationToken);

        var userId = result.UserId;
        await _teamEventPublisher.MembershipChangedAsync(teamId, userId, "Left", cancellationToken);
        return result.Team;
    }

    public async Task<TeamInviteDTO> RespondToInviteAsync(Guid teamId, Guid userId, bool accept, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithDurableEventTransactionAsync(
            () => _inner.RespondToInviteAsync(teamId, userId, accept, cancellationToken),
            invite => accept
                ? PublishTeamMemberAddedAsync(invite.TeamId, invite.UserId, cancellationToken)
                : Task.CompletedTask,
            cancellationToken);
    }

    public async Task<TeamInviteDTO> RespondToInviteAsync(string auth0UserId, Guid inviteId, bool accept, CancellationToken cancellationToken = default)
    {
        var invite = await ExecuteWithDurableEventTransactionAsync(
            () => _inner.RespondToInviteAsync(auth0UserId, inviteId, accept, cancellationToken),
            invite => accept
                ? PublishTeamMemberAddedAsync(invite.TeamId, invite.UserId, cancellationToken)
                : Task.CompletedTask,
            cancellationToken);

        await _teamEventPublisher.InviteChangedAsync(invite.TeamId, invite.Id, invite.UserId, invite.Status, cancellationToken);
        if (accept)
            await _teamEventPublisher.MembershipChangedAsync(invite.TeamId, invite.UserId, "Joined", cancellationToken);

        return invite;
    }

    public async Task<TeamManagementSummaryDTO> TransferCaptainAsync(string auth0UserId, Guid teamId, Guid newCaptainUserId, CancellationToken cancellationToken = default)
    {
        var team = await ExecuteWithDurableEventTransactionAsync(
            () => _inner.TransferCaptainAsync(auth0UserId, teamId, newCaptainUserId, cancellationToken),
            _ => PublishTeamCaptainTransferredAsync(teamId, newCaptainUserId, cancellationToken),
            cancellationToken);

        await _teamEventPublisher.CaptainTransferredAsync(teamId, newCaptainUserId, cancellationToken);
        return team;
    }

    public Task<TeamLogoResponseDTO> UploadTeamLogoAsync(string auth0UserId, Guid teamId, IFormFile logo, CancellationToken cancellationToken = default)
    {
        return _inner.UploadTeamLogoAsync(auth0UserId, teamId, logo, cancellationToken);
    }

    public Task<TeamLogoResponseDTO> RemoveTeamLogoAsync(string auth0UserId, Guid teamId, CancellationToken cancellationToken = default)
    {
        return _inner.RemoveTeamLogoAsync(auth0UserId, teamId, cancellationToken);
    }

    public Task<IEnumerable<GetTeamDTO>> SearchTeamsByNameAsync(string query, int? limit = null, CancellationToken cancellationToken = default)
    {
        return _inner.SearchTeamsByNameAsync(query, limit, cancellationToken);
    }

    public async Task<GetTeamDTO> UpdateTeamAsync(Guid id, UpdateTeamDTO teamDTO, CancellationToken cancellationToken = default)
    {
        var result = await ExecuteWithDurableEventTransactionAsync(
            async () =>
            {
                var currentTeam = await _dbContext.Teams
                    .AsNoTracking()
                    .Where(team => team.Id == id)
                    .Select(team => new
                    {
                        team.NormalizedName,
                        team.CaptainUserId
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                var team = await _inner.UpdateTeamAsync(id, teamDTO, cancellationToken);

                var shouldPublishRenamed =
                    currentTeam is not null &&
                    teamDTO.Name is not null &&
                    !string.Equals(currentTeam.NormalizedName, Team.NormalizeName(teamDTO.Name), StringComparison.Ordinal);

                var transferredCaptainUserId =
                    currentTeam is not null &&
                    teamDTO.CaptainUserId.HasValue &&
                    teamDTO.CaptainUserId.Value != currentTeam.CaptainUserId
                        ? teamDTO.CaptainUserId
                        : null;

                return new TeamUpdatedResult(team, shouldPublishRenamed, transferredCaptainUserId);
            },
            async result =>
            {
                if (result.NameChanged)
                    await PublishTeamRenamedAsync(id, cancellationToken);

                if (result.TransferredCaptainUserId.HasValue)
                    await PublishTeamCaptainTransferredAsync(id, result.TransferredCaptainUserId.Value, cancellationToken);
            },
            cancellationToken);

        return result.Team;
    }

    private async Task ExecuteWithDurableEventTransactionAsync(Func<Task> operation, Func<Task> publishDurableEvents, CancellationToken cancellationToken)
    {
        await ExecuteWithDurableEventTransactionAsync(
            async () =>
            {
                await operation();
                return true;
            },
            _ => publishDurableEvents(),
            cancellationToken);
    }

    private async Task<TResult> ExecuteWithDurableEventTransactionAsync<TResult>(
        Func<Task<TResult>> operation,
        Func<TResult, Task> publishDurableEvents,
        CancellationToken cancellationToken)
    {
        await using var transaction = await BeginDurableEventTransactionAsync(cancellationToken);

        try
        {
            var result = await operation();
            await publishDurableEvents(result);

            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);

            return result;
        }
        catch
        {
            if (transaction is not null)
                await transaction.RollbackAsync(cancellationToken);

            throw;
        }
    }

    private async Task<IDbContextTransaction?> BeginDurableEventTransactionAsync(CancellationToken cancellationToken)
    {
        if (!_dbContext.Database.IsRelational())
            return null;

        return await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    private async Task<TResult> PublishExpiredInviteEventsAroundAsync<TResult>(
        Func<Task<TResult>> operation,
        Guid? teamId = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var candidates = await GetExpiredInviteEventCandidatesAsync(teamId, userId, cancellationToken);

        ExceptionDispatchInfo? operationException = null;
        TResult? result = default;

        try
        {
            result = await operation();
        }
        catch (Exception exception)
        {
            operationException = ExceptionDispatchInfo.Capture(exception);
        }

        try
        {
            await PublishExpiredInviteEventsAsync(candidates, cancellationToken);
        }
        catch when (operationException is not null)
        {
        }

        operationException?.Throw();
        return result!;
    }

    private async Task<List<ExpiredTeamInviteChangedCandidate>> GetExpiredInviteEventCandidatesAsync(Guid? teamId, Guid? userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return await TeamInvites
            .AsNoTracking()
            .Where(invite =>
                invite.Status == TeamInviteStatus.Pending &&
                invite.ExpiresAt <= now &&
                (!teamId.HasValue || invite.TeamId == teamId.Value) &&
                (!userId.HasValue || invite.UserId == userId.Value))
            .Select(invite => new ExpiredTeamInviteChangedCandidate(
                invite.TeamId,
                invite.Id,
                invite.UserId,
                nameof(TeamInviteStatus.Expired)))
            .ToListAsync(cancellationToken);
    }

    private async Task PublishExpiredInviteEventsAsync(IReadOnlyCollection<ExpiredTeamInviteChangedCandidate> candidates, CancellationToken cancellationToken)
    {
        if (candidates.Count == 0)
            return;

        var candidateIds = candidates.Select(candidate => candidate.InviteId).ToList();
        var expiredInviteIds = await TeamInvites
            .AsNoTracking()
            .Where(invite => candidateIds.Contains(invite.Id) && invite.Status == TeamInviteStatus.Expired)
            .Select(invite => invite.Id)
            .ToListAsync(cancellationToken);

        foreach (var candidate in candidates.Where(candidate => expiredInviteIds.Contains(candidate.InviteId)))
        {
            await _teamEventPublisher.InviteChangedAsync(
                candidate.TeamId,
                candidate.InviteId,
                candidate.UserId,
                candidate.Status,
                cancellationToken);
        }
    }

    private async Task PublishTeamCreatedAsync(Guid teamId, Guid captainUserId, CancellationToken cancellationToken)
    {
        var team = await GetTeamForEventAsync(teamId, cancellationToken);
        IncrementTeamVersion(team);
        _moduleEventPublisher?.Publish(new TeamCreatedIntegrationEvent(
            new TeamId(team.Id),
            team.Version,
            team.Name,
            new UserId(captainUserId)));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task PublishTeamRenamedAsync(Guid teamId, CancellationToken cancellationToken)
    {
        var team = await GetTeamForEventAsync(teamId, cancellationToken);
        IncrementTeamVersion(team);
        _moduleEventPublisher?.Publish(new TeamRenamedIntegrationEvent(
            new TeamId(team.Id),
            team.Version,
            team.Name));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task PublishTeamDeletedAsync(Guid teamId, CancellationToken cancellationToken)
    {
        var team = await GetTeamForEventAsync(teamId, cancellationToken);
        IncrementTeamVersion(team);
        _moduleEventPublisher?.Publish(new TeamDeletedIntegrationEvent(
            new TeamId(team.Id),
            team.Version));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task PublishTeamMemberAddedAsync(Guid teamId, Guid userId, CancellationToken cancellationToken)
    {
        var team = await GetTeamForEventAsync(teamId, cancellationToken);
        IncrementTeamVersion(team);
        _moduleEventPublisher?.Publish(new TeamMemberAddedIntegrationEvent(
            new TeamId(team.Id),
            team.Version,
            new UserId(userId)));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task PublishTeamMemberRemovedAsync(Guid teamId, Guid userId, CancellationToken cancellationToken)
    {
        var team = await GetTeamForEventAsync(teamId, cancellationToken);
        IncrementTeamVersion(team);
        _moduleEventPublisher?.Publish(new TeamMemberRemovedIntegrationEvent(
            new TeamId(team.Id),
            team.Version,
            new UserId(userId)));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task PublishTeamCaptainTransferredAsync(Guid teamId, Guid newCaptainUserId, CancellationToken cancellationToken)
    {
        var team = await GetTeamForEventAsync(teamId, cancellationToken);
        IncrementTeamVersion(team);
        _moduleEventPublisher?.Publish(new TeamCaptainTransferredIntegrationEvent(
            new TeamId(team.Id),
            team.Version,
            new UserId(newCaptainUserId)));
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Team> GetTeamForEventAsync(Guid teamId, CancellationToken cancellationToken)
    {
        return await _dbContext.Teams.FindAsync([teamId], cancellationToken)
            ?? throw new InvalidOperationException($"Team '{teamId}' was not found after a successful team mutation.");
    }

    private async Task<Guid> GetCurrentUserIdAsync(string auth0UserId, CancellationToken cancellationToken)
    {
        var normalizedAuth0UserId = auth0UserId.Trim();
        return await _dbContext.Users
            .Where(user => user.Auth0UserId == normalizedAuth0UserId && !user.IsDeleted)
            .Select(user => user.Id)
            .FirstAsync(cancellationToken);
    }

    private static void IncrementTeamVersion(Team team)
    {
        team.Version++;
    }

    private sealed record TeamMemberRemovedResult(TeamManagementSummaryDTO Team, Guid UserId);

    private sealed record TeamUpdatedResult(
        GetTeamDTO Team,
        bool NameChanged,
        Guid? TransferredCaptainUserId);

    private sealed record ExpiredTeamInviteChangedCandidate(
        Guid TeamId,
        Guid InviteId,
        Guid UserId,
        string Status);
}
