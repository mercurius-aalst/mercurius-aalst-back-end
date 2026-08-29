using Mercurius.Modules.Tournament.Application.DTOs.Matches;
using Mercurius.Modules.Tournament.Domain;
using Mercurius.Modules.Tournament.Infrastructure;
using Mercurius.Modules.Identity.Contracts;
using Mercurius.Modules.Shared;
using Mercurius.Modules.Shared.Exceptions;
using Mercurius.Modules.Teams.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Platform.Eventing;
using MatchCompletedIntegrationEvent = Mercurius.Modules.Tournament.Contracts.MatchCompletedIntegrationEvent;
using MatchResolutionRequiredIntegrationEvent = Mercurius.Modules.Tournament.Contracts.MatchResolutionRequiredIntegrationEvent;
using MatchResultReversedIntegrationEvent = Mercurius.Modules.Tournament.Contracts.MatchResultReversedIntegrationEvent;
using MatchParticipantSide = Mercurius.Modules.Tournament.Contracts.MatchParticipantSide;

namespace Mercurius.Modules.Tournament.Application.Services;

internal sealed class MatchService : IMatchService
{
    private readonly ITournamentDbContext _dbContext;
    private readonly IIdentityModule _identityModule;
    private readonly ITeamsModule _teamsModule;
    private readonly IModuleEventPublisher _moduleEventPublisher;
    private readonly TimeProvider _timeProvider;

    public MatchService(
        ITournamentDbContext dbContext,
        IIdentityModule identityModule,
        ITeamsModule teamsModule,
        IModuleEventPublisher moduleEventPublisher,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _identityModule = identityModule;
        _teamsModule = teamsModule;
        _moduleEventPublisher = moduleEventPublisher;
        _timeProvider = timeProvider;
    }

    public async Task<GetMatchDTO> GetMatchByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var (tournament, match) = await GetMatchReadAsync(id, cancellationToken);
        if (HasExpiredDeadline(match, UtcNow()))
        {
            (tournament, match) = await GetMatchMutationGraphAsync(id, cancellationToken);
            await ApplyDeadlineAndPersistAsync(tournament, match, cancellationToken);
        }

        return TournamentDtoMapper.ToGetMatchDto(match);
    }

    public async Task<GetMatchActionStateDTO> GetMatchActionStateAsync(
        Guid id,
        string auth0UserId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var userId = await GetCurrentUserIdAsync(auth0UserId, cancellationToken);
        var (tournament, match) = await GetMatchReadAsync(id, cancellationToken);
        if (HasExpiredDeadline(match, UtcNow()))
        {
            (tournament, match) = await GetMatchMutationGraphAsync(id, cancellationToken);
            await ApplyDeadlineAndPersistAsync(tournament, match, cancellationToken);
        }

        var side = await FindParticipantSideAsync(match, userId, cancellationToken);
        var canViewPrivateReports = isAdmin &&
            (!tournament.AssignedAdminUserId.HasValue || tournament.AssignedAdminUserId.Value == userId);
        var canResolve = isAdmin &&
            tournament.Status == TournamentStatus.InProgress &&
            canViewPrivateReports &&
            match.LifecycleState is MatchLifecycleState.Disputed or MatchLifecycleState.AdminResolutionRequired;
        var resolveBlockedReason = !isAdmin
            ? "admin_required"
            : tournament.Status != TournamentStatus.InProgress
                ? "tournament_not_in_progress"
                : !canViewPrivateReports
                    ? "admin_not_assigned"
                    : "match_not_disputed";
        var hasBothParticipants = match.GetParticipant1Id().HasValue && match.GetParticipant2Id().HasValue;
        var canForceForfeit = isAdmin &&
            tournament.Status == TournamentStatus.InProgress &&
            hasBothParticipants &&
            !match.HasResult &&
            match.LifecycleState != MatchLifecycleState.AdminResolutionRequired;
        var forceForfeitBlockedReason = !isAdmin
            ? "admin_required"
            : tournament.Status != TournamentStatus.InProgress
                ? "tournament_not_in_progress"
                : match.HasResult
                    ? "match_already_completed"
                    : !hasBothParticipants
                        ? "match_not_ready"
                    : "match_requires_admin_resolution";
        var canReverse = isAdmin && tournament.Status == TournamentStatus.InProgress && match.HasResult;
        var reverseBlockedReason = !isAdmin
            ? "admin_required"
            : tournament.Status != TournamentStatus.InProgress
                ? "tournament_not_in_progress"
                : !match.HasResult
                    ? "match_not_completed"
                    : null;
        return TournamentDtoMapper.ToGetMatchActionStateDto(
            match,
            side,
            tournament.Status == TournamentStatus.InProgress,
            canViewPrivateReports,
            canResolve,
            canResolve ? null : resolveBlockedReason,
            canForceForfeit,
            canForceForfeit ? null : forceForfeitBlockedReason,
            canReverse,
            canReverse ? null : reverseBlockedReason);
    }

    public async Task<GetMatchDTO> ConfirmEndedAsync(
        Guid id,
        string auth0UserId,
        CancellationToken cancellationToken = default)
    {
        var userId = await GetCurrentUserIdAsync(auth0UserId, cancellationToken);
        await using var transaction = await BeginTransactionIfSupportedAsync(cancellationToken);
        var (tournament, match) = await GetMatchMutationGraphAsync(id, cancellationToken);
        EnsureInProgress(tournament);
        var side = await RequireParticipantSideAsync(match, userId, cancellationToken);
        var now = UtcNow();
        ApplyDeadline(tournament, match, now);
        match.ConfirmEnded((int)side, now);
        await SaveAndCommitAsync(transaction, cancellationToken);
        return TournamentDtoMapper.ToGetMatchDto(match);
    }

    public async Task<GetMatchDTO> SubmitScoreAsync(
        Guid id,
        string auth0UserId,
        SubmitMatchScoreDTO request,
        CancellationToken cancellationToken = default)
    {
        var userId = await GetCurrentUserIdAsync(auth0UserId, cancellationToken);
        await using var transaction = await BeginTransactionIfSupportedAsync(cancellationToken);
        var (tournament, match) = await GetMatchMutationGraphAsync(id, cancellationToken);
        EnsureInProgress(tournament);
        var side = await RequireParticipantSideAsync(match, userId, cancellationToken);
        var wasResult = match.HasResult;
        var now = UtcNow();
        ApplyDeadline(tournament, match, now);
        match.SubmitScore((int)side, request.Participant1Score, request.Participant2Score, now);
        if (!wasResult && match.HasResult)
        {
            match.ResultRecordedByUserId = userId;
            PublishCompletion(match);
        }

        await SaveAndCommitAsync(transaction, cancellationToken);
        return TournamentDtoMapper.ToGetMatchDto(match);
    }

    public async Task<GetMatchDTO> ForfeitAsync(
        Guid id,
        string auth0UserId,
        ForfeitMatchDTO request,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        var userId = await GetCurrentUserIdAsync(auth0UserId, cancellationToken);
        await using var transaction = await BeginTransactionIfSupportedAsync(cancellationToken);
        var (tournament, match) = await GetMatchMutationGraphAsync(id, cancellationToken);
        EnsureInProgress(tournament);

        var actorSide = await FindParticipantSideAsync(match, userId, cancellationToken);
        var targetSide = request.Participant;
        if (!isAdmin)
        {
            if (!actorSide.HasValue)
                throw new UnauthorizedAccessException("Only a participant or team captain can forfeit this match.");
            if (targetSide.HasValue && targetSide != actorSide)
                throw new UnauthorizedAccessException("Participants may only forfeit their own side.");
            targetSide = actorSide;
        }
        else if (!targetSide.HasValue)
        {
            if (!actorSide.HasValue)
                throw new ValidationException("An administrator must select a participant side to forfeit.");
            targetSide = actorSide;
        }

        var wasResult = match.HasResult;
        var now = UtcNow();
        ApplyDeadline(tournament, match, now);
        match.Forfeit((int)targetSide.Value, now);
        if (!wasResult)
        {
            match.ResultRecordedByUserId = userId;
            PublishCompletion(match);
        }

        await SaveAndCommitAsync(transaction, cancellationToken);
        return TournamentDtoMapper.ToGetMatchDto(match);
    }

    public async Task<GetMatchDTO> ResolveAsync(
        Guid id,
        string auth0UserId,
        ResolveMatchDTO request,
        CancellationToken cancellationToken = default)
    {
        var userId = await GetCurrentUserIdAsync(auth0UserId, cancellationToken);
        await using var transaction = await BeginTransactionIfSupportedAsync(cancellationToken);
        var (tournament, match) = await GetMatchMutationGraphAsync(id, cancellationToken);
        EnsureInProgress(tournament);
        EnsureAssignedAdmin(tournament, userId);
        var wasResult = match.HasResult;
        var now = UtcNow();
        ApplyDeadline(tournament, match, now);
        match.ResolveScore(request.Participant1Score, request.Participant2Score, now);
        match.ResultRecordedByUserId = userId;
        if (!wasResult)
            PublishCompletion(match);
        await SaveAndCommitAsync(transaction, cancellationToken);
        return TournamentDtoMapper.ToGetMatchDto(match);
    }

    public async Task<GetMatchDTO> ReverseAsync(
        Guid id,
        string auth0UserId,
        CancellationToken cancellationToken = default)
    {
        var userId = await GetCurrentUserIdAsync(auth0UserId, cancellationToken);
        await using var transaction = await BeginTransactionIfSupportedAsync(cancellationToken);
        var (tournament, match) = await GetMatchMutationGraphAsync(id, cancellationToken);
        await LoadDownstreamMatchesAsync(match, cancellationToken);
        EnsureInProgress(tournament);
        var downstream = GetDownstreamMatches(match);
        var blockingMatch = downstream.FirstOrDefault(HasPlayedResult);
        if (blockingMatch is not null)
            throw new ConflictException(
                "match_reversal_blocked",
                $"This result cannot be reversed because linked match {blockingMatch.Id} already has a result.");

        var winnerId = match.GetWinnerId();
        var loserId = match.GetLoserForMutation();
        foreach (var nextMatch in downstream)
        {
            if (winnerId.HasValue)
                nextMatch.ClearParticipant(winnerId.Value);
            if (loserId.HasValue)
                nextMatch.ClearParticipant(loserId.Value);
        }

        match.ReverseResult(UtcNow());
        match.ResultRecordedByUserId = userId;
        _moduleEventPublisher.Publish(new MatchResultReversedIntegrationEvent(
            new MatchId(match.Id),
            new TournamentId(match.TournamentId)));
        await SaveAndCommitAsync(transaction, cancellationToken);
        return TournamentDtoMapper.ToGetMatchDto(match);
    }

    public async Task<GetMatchDTO> UpdateMatchAsync(
        Guid id,
        UpdateMatchDTO updateMatchDTO,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await BeginTransactionIfSupportedAsync(cancellationToken);
        var (tournament, match) = await GetMatchMutationGraphAsync(id, cancellationToken);
        var wasResult = match.HasResult;
        var now = UtcNow();
        match.ForceCompleteScore(updateMatchDTO.Participant1Score, updateMatchDTO.Participant2Score, now);
        if (!wasResult)
            PublishCompletion(match);
        await SaveAndCommitAsync(transaction, cancellationToken);
        return TournamentDtoMapper.ToGetMatchDto(match);
    }

    private async Task<(TournamentAggregate Tournament, Match Match)> GetMatchReadAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var match = await CreateMatchReadQuery(id)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (match is null)
            throw new NotFoundException("Match not found.");

        return (match.Tournament, match);
    }

    private async Task<(TournamentAggregate Tournament, Match Match)> GetMatchMutationGraphAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var match = await CreateMatchMutationQuery(id)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (match is null)
            throw new NotFoundException("Match not found.");

        return (match.Tournament, match);
    }

    private IQueryable<Match> CreateMatchReadQuery(Guid id) =>
        _dbContext.Matches
            .AsNoTracking()
            .Include(candidate => candidate.Tournament)
            .Where(candidate => candidate.Id == id);

    private IQueryable<Match> CreateMatchMutationQuery(Guid id) =>
        _dbContext.Matches
            .Include(candidate => candidate.Tournament)
            .Include(candidate => candidate.WinnerNextMatch)
            .Include(candidate => candidate.LoserNextMatch)
            .Where(candidate => candidate.Id == id);

    private async Task LoadDownstreamMatchesAsync(
        Match source,
        CancellationToken cancellationToken)
    {
        var seen = new HashSet<Guid> { source.Id };
        var pending = new Queue<(Match Parent, Guid ChildId, bool IsWinnerLink)>();
        EnqueueDownstreamLinks(source, pending);

        while (pending.Count != 0)
        {
            var batch = new List<(Match Parent, Guid ChildId, bool IsWinnerLink)>();
            while (pending.Count != 0)
                batch.Add(pending.Dequeue());

            var children = await _dbContext.Matches
                .Where(candidate => batch.Select(link => link.ChildId).Contains(candidate.Id))
                .ToListAsync(cancellationToken);
            var childrenById = children.ToDictionary(candidate => candidate.Id);
            foreach (var link in batch)
            {
                if (!childrenById.TryGetValue(link.ChildId, out var child))
                    continue;

                if (link.IsWinnerLink)
                    link.Parent.WinnerNextMatch = child;
                else
                    link.Parent.LoserNextMatch = child;

                if (seen.Add(child.Id))
                    EnqueueDownstreamLinks(child, pending);
            }
        }
    }

    private static void EnqueueDownstreamLinks(
        Match match,
        Queue<(Match Parent, Guid ChildId, bool IsWinnerLink)> pending)
    {
        if (match.WinnerNextMatchId.HasValue)
            pending.Enqueue((match, match.WinnerNextMatchId.Value, true));
        if (match.LoserNextMatchId.HasValue)
            pending.Enqueue((match, match.LoserNextMatchId.Value, false));
    }

    private static bool HasExpiredDeadline(Match match, DateTime nowUtc) =>
        (match.LifecycleState == MatchLifecycleState.ScoreConfirmation &&
         match.ScoreConfirmationDeadlineUtc is { } scoreDeadline && nowUtc >= scoreDeadline) ||
        (match.LifecycleState == MatchLifecycleState.Disputed &&
         match.CorrectionDeadlineUtc is { } correctionDeadline && nowUtc >= correctionDeadline);

    private async Task ApplyDeadlineAndPersistAsync(
        TournamentAggregate tournament,
        Match match,
        CancellationToken cancellationToken)
    {
        var beforeState = match.LifecycleState;
        var beforeResult = match.HasResult;
        ApplyDeadline(tournament, match, UtcNow());
        if (beforeState == match.LifecycleState && beforeResult == match.HasResult)
            return;

        if (!beforeResult && match.HasResult)
            PublishCompletion(match);
        await SaveAndCommitAsync(null, cancellationToken);
    }

    private void ApplyDeadline(
        TournamentAggregate tournament,
        Match match,
        DateTime nowUtc)
    {
        var beforeState = match.LifecycleState;
        match.ApplyDeadline(nowUtc);
        if (beforeState != MatchLifecycleState.AdminResolutionRequired &&
            match.LifecycleState == MatchLifecycleState.AdminResolutionRequired)
        {
            _moduleEventPublisher.Publish(new MatchResolutionRequiredIntegrationEvent(
                new MatchId(match.Id),
                new TournamentId(match.TournamentId),
                tournament.AssignedAdminUserId));
        }
    }

    private void PublishCompletion(Match match)
    {
        var winnerId = match.GetWinnerId();
        if (!winnerId.HasValue)
            return;
        _moduleEventPublisher.Publish(new MatchCompletedIntegrationEvent(
            new MatchId(match.Id),
            new TournamentId(match.TournamentId),
            winnerId.Value));
    }

    private async Task<Guid> GetCurrentUserIdAsync(string auth0UserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(auth0UserId))
            throw new UnauthorizedAccessException("Authenticated user id is missing.");
        var user = await _identityModule.GetUserProfileByAuth0IdAsync(auth0UserId.Trim(), cancellationToken);
        if (user is null || user.IsDeleted)
            throw new NotFoundException("Current user profile was not found.");
        return user.Id.Value;
    }

    private async Task<MatchParticipantSide?> FindParticipantSideAsync(
        Match match,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (match.ParticipationMode == ParticipationMode.Individual)
        {
            if (match.UserParticipant1Id == userId)
                return MatchParticipantSide.Participant1;
            if (match.UserParticipant2Id == userId)
                return MatchParticipantSide.Participant2;
            return null;
        }

        foreach (var (teamId, side) in new[]
        {
            (match.TeamParticipant1Id, MatchParticipantSide.Participant1),
            (match.TeamParticipant2Id, MatchParticipantSide.Participant2)
        })
        {
            if (!teamId.HasValue)
                continue;
            var team = await _teamsModule.GetTeamSummaryAsync(new TeamId(teamId.Value), cancellationToken);
            if (team is not null && !team.IsDeleted && team.CaptainUserId?.Value == userId)
                return side;
        }
        return null;
    }

    private async Task<MatchParticipantSide> RequireParticipantSideAsync(
        Match match,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var side = await FindParticipantSideAsync(match, userId, cancellationToken);
        if (!side.HasValue)
            throw new UnauthorizedAccessException("Only a participant or team captain can perform this action.");
        return side.Value;
    }

    private static void EnsureInProgress(TournamentAggregate tournament)
    {
        if (tournament.Status != TournamentStatus.InProgress)
            throw new ValidationException("Match actions are available only while the tournament is in progress.");
    }

    private static void EnsureAssignedAdmin(TournamentAggregate tournament, Guid userId)
    {
        if (tournament.AssignedAdminUserId.HasValue && tournament.AssignedAdminUserId.Value != userId)
            throw new ForbiddenException(
                "admin_not_assigned",
                "Only the assigned tournament administrator can resolve this match.");
    }

    private static bool HasPlayedResult(Match match) =>
        match.HasResult ||
        match.Participant1Score.HasValue ||
        match.Participant2Score.HasValue ||
        match.HasWinner() ||
        match.GetLoserForMutation().HasValue ||
        match.ForfeitedParticipantNumber.HasValue;

    private static IReadOnlyList<Match> GetDownstreamMatches(Match source)
    {
        var seen = new HashSet<Guid> { source.Id };
        var queue = new Queue<Match>();
        if (source.WinnerNextMatch is not null)
            queue.Enqueue(source.WinnerNextMatch);
        if (source.LoserNextMatch is not null)
            queue.Enqueue(source.LoserNextMatch);

        var result = new List<Match>();
        while (queue.Count != 0)
        {
            var current = queue.Dequeue();
            if (!seen.Add(current.Id))
                continue;
            result.Add(current);
            if (current.WinnerNextMatch is not null)
                queue.Enqueue(current.WinnerNextMatch);
            if (current.LoserNextMatch is not null)
                queue.Enqueue(current.LoserNextMatch);
        }
        return result;
    }

    private DateTime UtcNow() => _timeProvider.GetUtcNow().UtcDateTime;

    private async Task<IDbContextTransaction?> BeginTransactionIfSupportedAsync(
        CancellationToken cancellationToken)
    {
        if (_dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            return null;
        return await _dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    private async Task SaveAndCommitAsync(
        IDbContextTransaction? transaction,
        CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
                await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(
                "match_state_changed",
                "The match changed while this action was being processed. Refresh and try again.");
        }
    }
}
