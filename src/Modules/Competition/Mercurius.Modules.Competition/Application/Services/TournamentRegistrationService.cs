using Mercurius.Modules.Competition.Application;
using Mercurius.Modules.Competition.Application.DTOs.Participants;
using Mercurius.Modules.Competition.Application.DTOs.Registrations;
using Mercurius.Modules.Competition.Domain;
using Mercurius.Modules.Competition.Infrastructure;
using Mercurius.Modules.Identity.Contracts;
using Mercurius.Modules.Shared;
using Mercurius.Modules.Shared.Exceptions;
using Mercurius.Modules.Teams.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Platform.Eventing;
using RosterMemberConfirmedIntegrationEvent =
    Mercurius.Modules.Competition.Contracts.RosterMemberConfirmedIntegrationEvent;
using TournamentRegistrationCanceledIntegrationEvent =
    Mercurius.Modules.Competition.Contracts.TournamentRegistrationCanceledIntegrationEvent;
using TournamentRegistrationCreatedIntegrationEvent =
    Mercurius.Modules.Competition.Contracts.TournamentRegistrationCreatedIntegrationEvent;
using TournamentRosterConfirmationChangedEvent =
    Mercurius.Modules.Competition.Contracts.TournamentRosterConfirmationChangedEvent;

namespace Mercurius.Modules.Competition.Application.Services;

internal sealed class TournamentRegistrationService : ITournamentRegistrationService
{
    private readonly ICompetitionDbContext _dbContext;
    private readonly IIdentityModule _identityModule;
    private readonly ITeamsModule _teamsModule;
    private readonly CompetitionEligibilityEvaluator _eligibilityEvaluator;
    private readonly RegistrationMappingContextBuilder _contextBuilder;
    private readonly TournamentRegistrationPersistenceCoordinator _persistenceCoordinator;
    private readonly TournamentRegistrationReadModelService _readModelService;
    private readonly CompetitionDtoMapper _mapper;
    private readonly ICompetitionRealtimePublisher _realtimePublisher;
    private readonly IModuleEventPublisher _moduleEventPublisher;

    public TournamentRegistrationService(
        ICompetitionDbContext dbContext,
        IIdentityModule identityModule,
        ITeamsModule teamsModule,
        CompetitionEligibilityEvaluator eligibilityEvaluator,
        RegistrationMappingContextBuilder contextBuilder,
        TournamentRegistrationPersistenceCoordinator persistenceCoordinator,
        TournamentRegistrationReadModelService readModelService,
        CompetitionDtoMapper mapper,
        ICompetitionRealtimePublisher realtimePublisher,
        IModuleEventPublisher moduleEventPublisher)
    {
        _dbContext = dbContext;
        _identityModule = identityModule;
        _teamsModule = teamsModule;
        _eligibilityEvaluator = eligibilityEvaluator;
        _contextBuilder = contextBuilder;
        _persistenceCoordinator = persistenceCoordinator;
        _readModelService = readModelService;
        _mapper = mapper;
        _realtimePublisher = realtimePublisher;
        _moduleEventPublisher = moduleEventPublisher;
    }

    public async Task<EligibilityResponseDTO> CheckIndividualEligibilityAsync(
        string auth0UserId,
        Guid gameId,
        CancellationToken cancellationToken = default)
    {
        var userId = await GetCurrentUserIdAsync(auth0UserId, cancellationToken);
        var game = await GetGameAsync(gameId, cancellationToken);
        var reasons = await _eligibilityEvaluator.GetIndividualCompetitionFailuresAsync(game, userId, null, cancellationToken);
        return new EligibilityResponseDTO(reasons.Count == 0, reasons);
    }

    public async Task<EligibilityResponseDTO> CheckTeamEligibilityAsync(
        string auth0UserId,
        Guid gameId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        var userId = await GetCurrentUserIdAsync(auth0UserId, cancellationToken);
        var game = await GetGameAsync(gameId, cancellationToken);
        var team = await GetTeamWithMembersAsync(teamId, cancellationToken);
        var reasons = await GetTeamEligibilityFailuresAsync(game, team, userId, null, cancellationToken);
        return new EligibilityResponseDTO(reasons.Count == 0, reasons);
    }

    public async Task<RosterCandidateEligibilityResponseDTO> CheckRosterEligibilityAsync(
        string auth0UserId,
        Guid gameId,
        Guid teamId,
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var userId = await GetCurrentUserIdAsync(auth0UserId, cancellationToken);
        var game = await GetGameAsync(gameId, cancellationToken);
        var team = await GetTeamWithMembersAsync(teamId, cancellationToken);
        var reasons = await GetTeamEligibilityFailuresAsync(game, team, userId, null, cancellationToken);
        reasons.AddRange(CompetitionEligibilityEvaluator.GetRosterSizeFailures(game, userIds));

        var distinctUserIds = userIds.Distinct().ToList();
        var users = await GetActiveUserProfilesByIdAsync(distinctUserIds, cancellationToken);
        var candidateFailures = await GetRosterCandidateFailuresAsync(game.Id, team, distinctUserIds, users, null, cancellationToken);

        var candidateResults = new List<RosterCandidateEligibilityDTO>();
        foreach (var candidateId in distinctUserIds)
        {
            candidateResults.Add(new RosterCandidateEligibilityDTO(
                candidateId,
                users.TryGetValue(candidateId, out var candidate) ? new PublicUserDTO(candidate) : null,
                candidateFailures[candidateId].Count == 0,
                candidateFailures[candidateId]));
        }

        if (candidateResults.Any(candidate => !candidate.Eligible))
            reasons.Add("roster_candidate_ineligible");

        return new RosterCandidateEligibilityResponseDTO(reasons.Count == 0, reasons.Distinct().ToList(), candidateResults);
    }

    public async Task<TournamentRegistrationDTO> RegisterIndividualAsync(
        string auth0UserId,
        Guid gameId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _persistenceCoordinator.BeginTransactionIfSupportedAsync(cancellationToken);
        var userId = await GetCurrentUserIdAsync(auth0UserId, cancellationToken);
        var game = await GetGameAsync(gameId, cancellationToken);
        var reasons = await _eligibilityEvaluator.GetIndividualCompetitionFailuresAsync(game, userId, null, cancellationToken);
        if (reasons.Count != 0)
            throw new ValidationException(string.Join(", ", reasons));
        var userProfile = (await GetActiveUserProfilesByIdAsync([userId], cancellationToken))[userId];

        var now = DateTime.UtcNow;
        var registration = new TournamentRegistration
        {
            Id = Guid.NewGuid(),
            GameId = game.Id,
            Kind = TournamentRegistrationKind.Individual,
            Status = TournamentRegistrationStatus.Active,
            RegisteredByUserId = userId,
            RegisteredByUsernameAtRegistration = userProfile.Username ?? string.Empty,
            UserId = userId,
            UsernameAtRegistration = userProfile.Username,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.TournamentRegistrations.Add(registration);
        PublishRegistrationCreated(registration);
        await _persistenceCoordinator.SaveChangesAsync("User already has pending or active participation for this tournament.", cancellationToken);
        var dto = await _mapper.ToRegistrationDtoAsync(
            await GetRegistrationByIdAsync(registration.Id, cancellationToken),
            cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        return dto;
    }

    public async Task UnregisterIndividualAsync(
        string auth0UserId,
        Guid gameId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _persistenceCoordinator.BeginTransactionIfSupportedAsync(cancellationToken);
        var userId = await GetCurrentUserIdAsync(auth0UserId, cancellationToken);
        var game = await GetGameAsync(gameId, cancellationToken);
        EnsureScheduled(game);

        var registration = await _dbContext.TournamentRegistrations
            .FirstOrDefaultAsync(r =>
                r.GameId == gameId &&
                r.UserId == userId &&
                r.Kind == TournamentRegistrationKind.Individual &&
                r.Status == TournamentRegistrationStatus.Active,
                cancellationToken);
        if (registration is null)
            throw new NotFoundException("Individual registration not found.");

        _dbContext.TournamentRegistrations.Remove(registration);
        PublishRegistrationCanceled(registration);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }

    public async Task<TournamentRegistrationDTO> SubmitTeamRosterAsync(
        string auth0UserId,
        Guid gameId,
        SubmitTeamRosterDTO request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _persistenceCoordinator.BeginTransactionIfSupportedAsync(cancellationToken);
        var userId = await GetCurrentUserIdAsync(auth0UserId, cancellationToken);
        var game = await GetGameAsync(gameId, cancellationToken);
        var team = await GetTeamWithMembersAsync(request.TeamId, cancellationToken);
        var existing = await _dbContext.TournamentRegistrations
            .Include(registration => registration.RosterMembers)
            .FirstOrDefaultAsync(registration =>
                registration.GameId == gameId &&
                registration.TeamId == team.TeamId.Value &&
                registration.Kind == TournamentRegistrationKind.Team,
                cancellationToken);
        var excludedRegistrationId = existing?.Id;

        var teamFailures = await GetTeamEligibilityFailuresAsync(game, team, userId, excludedRegistrationId, cancellationToken);
        var rosterFailures = CompetitionEligibilityEvaluator.GetRosterSizeFailures(game, request.UserIds);
        if (!request.UserIds.Contains(userId))
            rosterFailures.Add("captain_required");
        var candidateProfiles = await GetActiveUserProfilesByIdAsync(
            request.UserIds.Distinct().ToArray(),
            cancellationToken);
        var candidateFailures = await GetRosterCandidateFailuresAsync(
            game.Id,
            team,
            request.UserIds.Distinct().ToList(),
            candidateProfiles,
            excludedRegistrationId,
            cancellationToken);
        foreach (var failure in candidateFailures.Values.SelectMany(static failures => failures))
            rosterFailures.Add(failure);

        var failures = teamFailures.Concat(rosterFailures).Distinct().ToList();
        if (failures.Count != 0)
            throw new ValidationException(string.Join(", ", failures));

        if (existing is not null)
            DeleteTransientTeamRegistration(existing);

        var now = DateTime.UtcNow;
        var registration = new TournamentRegistration
        {
            Id = Guid.NewGuid(),
            GameId = game.Id,
            Kind = TournamentRegistrationKind.Team,
            Status = TournamentRegistrationStatus.PendingConfirmation,
            RegisteredByUserId = userId,
            RegisteredByUsernameAtRegistration = candidateProfiles[userId].Username ?? string.Empty,
            TeamId = team.TeamId.Value,
            TeamNameAtRegistration = team.TeamName,
            TeamCaptainUserIdAtRegistration = team.CaptainUserId?.Value,
            TeamLogoUrlAtRegistration = team.LogoUrl,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        foreach (var memberId in request.UserIds.Distinct())
        {
            var isCaptain = memberId == team.CaptainUserId?.Value;
            var profile = candidateProfiles[memberId];
            registration.RosterMembers.Add(new TournamentRegistrationRosterMember
            {
                Id = Guid.NewGuid(),
                GameId = game.Id,
                TeamId = team.TeamId.Value,
                TeamNameAtRegistration = team.TeamName,
                UserId = memberId,
                UsernameAtRegistration = profile.Username ?? string.Empty,
                DisplayNameAtRegistration = profile.DisplayName,
                IsCaptain = isCaptain,
                ConfirmationStatus = isCaptain ? RosterMemberConfirmationStatus.AutoConfirmed : RosterMemberConfirmationStatus.Pending,
                ConfirmedAtUtc = isCaptain ? now : null,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        if (registration.RosterMembers.All(member => member.ConfirmationStatus != RosterMemberConfirmationStatus.Pending))
            registration.Activate(now);

        _dbContext.TournamentRegistrations.Add(registration);
        PublishRegistrationCreated(registration);
        var rosterConfirmationEvents = CreateRosterConfirmationEvents(registration);

        await _persistenceCoordinator.SaveChangesAsync("One or more roster members already has pending or active participation for this tournament.", cancellationToken);
        var dto = await _mapper.ToRegistrationDtoAsync(
            await GetRegistrationByIdAsync(registration.Id, cancellationToken),
            cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        await PublishRosterConfirmationEventsAsync(rosterConfirmationEvents, cancellationToken);
        return dto;
    }

    public async Task<TournamentRegistrationDTO> ConfirmRosterAsync(
        string auth0UserId,
        Guid gameId,
        Guid rosterMemberId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _persistenceCoordinator.BeginTransactionIfSupportedAsync(cancellationToken);
        var userId = await GetCurrentUserIdAsync(auth0UserId, cancellationToken);
        var member = await _dbContext.TournamentRegistrationRosterMembers
            .Include(roster => roster.TournamentRegistration)
                .ThenInclude(registration => registration.RosterMembers)
            .Include(roster => roster.TournamentRegistration)
                .ThenInclude(registration => registration.Game)
            .FirstOrDefaultAsync(
                roster => roster.Id == rosterMemberId && roster.GameId == gameId && roster.UserId == userId,
                cancellationToken);
        if (member is null || member.ConfirmationStatus != RosterMemberConfirmationStatus.Pending)
            throw new NotFoundException("Pending roster confirmation not found.");

        var registration = member.TournamentRegistration;
        EnsureScheduled(registration.Game);
        if (!registration.TeamId.HasValue)
            throw new ValidationException("Team registration is invalid.");
        var team = await GetTeamWithMembersAsync(registration.TeamId.Value, cancellationToken);

        var candidateFailures = await GetRosterCandidateFailuresAsync(
            registration.GameId,
            team,
            userId,
            null,
            registration.Id,
            cancellationToken);
        if (candidateFailures.Count != 0)
            throw new ValidationException(string.Join(", ", candidateFailures));

        var now = DateTime.UtcNow;
        member.Confirm(now);
        if (registration.RosterMembers.All(roster => roster.ConfirmationStatus is RosterMemberConfirmationStatus.AutoConfirmed or RosterMemberConfirmationStatus.Confirmed))
            registration.Activate(now);
        else
            registration.UpdatedAtUtc = now;

        _moduleEventPublisher.Publish(new RosterMemberConfirmedIntegrationEvent(
            new TournamentRegistrationId(registration.Id),
            new UserId(member.UserId),
            new TeamId(registration.TeamId!.Value)));
        await _persistenceCoordinator.SaveChangesAsync("User already has pending or active participation for this tournament.", cancellationToken);
        var dto = await _mapper.ToRegistrationDtoAsync(
            await GetRegistrationByIdAsync(registration.Id, cancellationToken),
            cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
        return dto;
    }

    public async Task UnregisterTeamAsync(
        string auth0UserId,
        Guid gameId,
        Guid teamId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _persistenceCoordinator.BeginTransactionIfSupportedAsync(cancellationToken);
        var userId = await GetCurrentUserIdAsync(auth0UserId, cancellationToken);
        var game = await GetGameAsync(gameId, cancellationToken);
        EnsureScheduled(game);
        var team = await GetTeamWithMembersAsync(teamId, cancellationToken);
        EnsureCaptain(team, userId);

        var registration = await GetTeamRegistrationForMutationAsync(gameId, teamId, cancellationToken);
        DeleteTransientTeamRegistration(registration);
        PublishRegistrationCanceled(registration);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }

    public async Task<CurrentUserTournamentRegistrationStateDTO> GetCurrentUserStateAsync(
        string auth0UserId,
        Guid gameId,
        CancellationToken cancellationToken = default)
    {
        var userId = await GetCurrentUserIdAsync(auth0UserId, cancellationToken);
        var game = await GetGameAsync(gameId, cancellationToken);
        return await _readModelService.GetCurrentUserStateAsync(userId, game, cancellationToken);
    }

    public async Task<IReadOnlyList<AdminTournamentRegistrationDTO>> GetAdminRegistrationsAsync(
        Guid gameId,
        CancellationToken cancellationToken = default)
    {
        _ = await GetGameAsync(gameId, cancellationToken);
        return await _readModelService.GetAdminRegistrationsAsync(gameId, cancellationToken);
    }

    public async Task RemoveIndividualAsAdminAsync(
        Guid gameId,
        Guid userId,
        string? reason,
        string? adminAuth0UserId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _persistenceCoordinator.BeginTransactionIfSupportedAsync(cancellationToken);
        _ = string.IsNullOrWhiteSpace(adminAuth0UserId)
            ? (Guid?)null
            : await GetCurrentUserIdAsync(adminAuth0UserId, cancellationToken);
        var registration = await _dbContext.TournamentRegistrations.FirstOrDefaultAsync(registration =>
            registration.GameId == gameId &&
            registration.UserId == userId &&
            registration.Kind == TournamentRegistrationKind.Individual,
            cancellationToken);
        if (registration is null)
            throw new NotFoundException("Individual registration not found.");

        _dbContext.TournamentRegistrations.Remove(registration);
        PublishRegistrationCanceled(registration);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }

    public async Task RemoveTeamAsAdminAsync(
        Guid gameId,
        Guid teamId,
        string? reason,
        string? adminAuth0UserId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _persistenceCoordinator.BeginTransactionIfSupportedAsync(cancellationToken);
        _ = string.IsNullOrWhiteSpace(adminAuth0UserId)
            ? (Guid?)null
            : await GetCurrentUserIdAsync(adminAuth0UserId, cancellationToken);
        var registration = await GetTeamRegistrationForMutationAsync(gameId, teamId, cancellationToken);
        DeleteTransientTeamRegistration(registration);
        PublishRegistrationCanceled(registration);
        await _dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
            await transaction.CommitAsync(cancellationToken);
    }

    private async Task<List<string>> GetTeamEligibilityFailuresAsync(
        Game game,
        TeamRosterSnapshot team,
        Guid captainUserId,
        Guid? excludedRegistrationId,
        CancellationToken cancellationToken)
    {
        var reasons = new List<string>();
        reasons.AddRange(await _eligibilityEvaluator.GetTeamCompetitionFailuresAsync(
            game,
            team.TeamId.Value,
            captainUserId,
            excludedRegistrationId,
            cancellationToken));
        if (team.IsDeleted)
            reasons.Add("team_deleted");
        if (team.CaptainUserId?.Value != captainUserId)
            reasons.Add("captain_required");
        return reasons;
    }

    private async Task<List<string>> GetRosterCandidateFailuresAsync(
        Guid gameId,
        TeamRosterSnapshot team,
        Guid userId,
        IReadOnlyDictionary<Guid, UserProfileSummary>? activeProfiles,
        Guid? excludedRegistrationId,
        CancellationToken cancellationToken)
    {
        var failures = await GetRosterCandidateFailuresAsync(
            gameId,
            team,
            [userId],
            activeProfiles,
            excludedRegistrationId,
            cancellationToken);
        return failures[userId];
    }

    private async Task<Dictionary<Guid, List<string>>> GetRosterCandidateFailuresAsync(
        Guid gameId,
        TeamRosterSnapshot team,
        IReadOnlyCollection<Guid> userIds,
        IReadOnlyDictionary<Guid, UserProfileSummary>? activeProfiles,
        Guid? excludedRegistrationId,
        CancellationToken cancellationToken)
    {
        var distinctUserIds = userIds.Distinct().ToList();
        if (distinctUserIds.Count == 0)
            return [];

        var activeUserIds = (activeProfiles ?? await GetActiveUserProfilesByIdAsync(distinctUserIds, cancellationToken))
            .Keys
            .ToHashSet();
        var teamMemberIds = team.Members.Select(member => member.UserId.Value).ToHashSet();

        var directParticipationUserIds = await _dbContext.TournamentRegistrations
            .AsNoTracking()
            .Where(registration =>
                registration.GameId == gameId &&
                registration.UserId.HasValue &&
                distinctUserIds.Contains(registration.UserId.Value) &&
                (!excludedRegistrationId.HasValue || registration.Id != excludedRegistrationId.Value))
            .Select(registration => registration.UserId!.Value)
            .ToListAsync(cancellationToken);

        var rosterParticipationUserIds = await _dbContext.TournamentRegistrationRosterMembers
            .AsNoTracking()
            .Where(member =>
                member.GameId == gameId &&
                distinctUserIds.Contains(member.UserId) &&
                (!excludedRegistrationId.HasValue || member.TournamentRegistrationId != excludedRegistrationId.Value))
            .Select(member => member.UserId)
            .ToListAsync(cancellationToken);
        var participatingUserIds = directParticipationUserIds
            .Concat(rosterParticipationUserIds)
            .ToHashSet();

        return distinctUserIds.ToDictionary(
            userId => userId,
            userId =>
            {
                var reasons = new List<string>();
                if (!activeUserIds.Contains(userId))
                    reasons.Add("user_not_found");
                if (!teamMemberIds.Contains(userId))
                    reasons.Add("not_team_member");
                if (participatingUserIds.Contains(userId))
                    reasons.Add("duplicate_participation");

                return reasons;
            });
    }

    private static List<TournamentRosterConfirmationChangedEvent> CreateRosterConfirmationEvents(
     TournamentRegistration registration)
    {
        return registration.RosterMembers
            .Where(member => member.ConfirmationStatus == RosterMemberConfirmationStatus.Pending)
            .Select(member => new TournamentRosterConfirmationChangedEvent(
                registration.TeamId!.Value,
                member.Id,
                member.UserId,
                nameof(RosterMemberConfirmationStatus.Pending)))
            .ToList();
    }

    private async Task PublishRosterConfirmationEventsAsync(
        IEnumerable<TournamentRosterConfirmationChangedEvent> rosterConfirmationEvents,
        CancellationToken cancellationToken)
    {
        foreach (var rosterConfirmationEvent in rosterConfirmationEvents)
        {
            await _realtimePublisher.RosterConfirmationChangedAsync(
                rosterConfirmationEvent.TeamId,
                rosterConfirmationEvent.RosterMemberId,
                rosterConfirmationEvent.UserId,
                rosterConfirmationEvent.Status,
                cancellationToken);
        }
    }

    private void DeleteTransientTeamRegistration(TournamentRegistration registration)
    {
        _dbContext.TournamentRegistrations.Remove(registration);
    }

    private void PublishRegistrationCreated(TournamentRegistration registration)
    {
        _moduleEventPublisher.Publish(new TournamentRegistrationCreatedIntegrationEvent(
            new TournamentRegistrationId(registration.Id),
            new GameId(registration.GameId),
            new UserId(registration.RegisteredByUserId),
            registration.TeamId.HasValue ? new TeamId(registration.TeamId.Value) : null));
    }

    private void PublishRegistrationCanceled(TournamentRegistration registration)
    {
        _moduleEventPublisher.Publish(new TournamentRegistrationCanceledIntegrationEvent(
            new TournamentRegistrationId(registration.Id),
            new GameId(registration.GameId),
            registration.TeamId.HasValue ? new TeamId(registration.TeamId.Value) : null));
    }

    private async Task<TournamentRegistration> GetTeamRegistrationForMutationAsync(
        Guid gameId,
        Guid teamId,
        CancellationToken cancellationToken)
    {
        var registration = await _dbContext.TournamentRegistrations
            .Include(r => r.RosterMembers)
            .FirstOrDefaultAsync(r =>
                r.GameId == gameId &&
                r.TeamId == teamId &&
                r.Kind == TournamentRegistrationKind.Team,
                cancellationToken);
        if (registration is null)
            throw new NotFoundException("Team registration not found.");
        return registration;
    }

    private Task<TournamentRegistration> GetRegistrationByIdAsync(
        Guid registrationId,
        CancellationToken cancellationToken)
    {
        return GetRegistrationQuery().FirstAsync(
            registration => registration.Id == registrationId,
            cancellationToken);
    }

    private IQueryable<TournamentRegistration> GetRegistrationQuery()
    {
        return _dbContext.TournamentRegistrations
            .Include(registration => registration.RosterMembers)
            ;
    }

    private async Task<Game> GetGameAsync(Guid gameId, CancellationToken cancellationToken)
    {
        var game = await _dbContext.Games.FindAsync([gameId], cancellationToken);
        if (game is null)
            throw new NotFoundException($"{nameof(Game)} not found");
        return game;
    }

    private async Task<TeamRosterSnapshot> GetTeamWithMembersAsync(
        Guid teamId,
        CancellationToken cancellationToken)
    {
        var team = await _teamsModule.GetTeamRosterSnapshotAsync(new TeamId(teamId), cancellationToken);
        if (team is null)
            throw new NotFoundException("Team not found");
        return team;
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

    private async Task<IReadOnlyDictionary<Guid, UserProfileSummary>> GetActiveUserProfilesByIdAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        var profiles = await _identityModule.GetUsersByIdsAsync(
            userIds.Select(userId => new UserId(userId)).ToArray(),
            cancellationToken);

        return profiles.Values
            .Where(user => !user.IsDeleted)
            .ToDictionary(user => user.Id.Value);
    }

    private static void EnsureScheduled(Game game)
    {
        if (game.Status != GameStatus.Scheduled)
            throw new ValidationException("Tournament must be scheduled for registration changes.");
    }

    private static void EnsureCaptain(TeamRosterSnapshot team, Guid userId)
    {
        if (team.CaptainUserId?.Value != userId)
            throw new UnauthorizedAccessException("Only the team captain can perform this action.");
    }

}
