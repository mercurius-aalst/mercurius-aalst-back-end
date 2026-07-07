using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.DTOs.UserDTOs;
using Mercurius.LAN.API.DTOs.RegistrationDTOs;
using Mercurius.Modules.Identity.Contracts;
using Mercurius.Modules.Shared;
using Mercurius.Modules.Shared.Exceptions;
using Mercurius.LAN.API.Models;
using Mercurius.Modules.Teams.Domain;
using Mercurius.Modules.Teams.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Mercurius.LAN.API.Services.RegistrationServices;

public class TournamentRegistrationService : ITournamentRegistrationService
{
    private readonly MercuriusDBContext _dbContext;
    private readonly ITeamEventPublisher _eventPublisher;
    private readonly IIdentityModule _identityModule;

    public TournamentRegistrationService(
        MercuriusDBContext dbContext,
        ITeamEventPublisher eventPublisher,
        IIdentityModule identityModule)
    {
        _dbContext = dbContext;
        _eventPublisher = eventPublisher;
        _identityModule = identityModule;
    }

    public async Task<EligibilityResponseDTO> CheckIndividualEligibilityAsync(string auth0UserId, Guid gameId)
    {
        var userId = await GetCurrentUserIdAsync(auth0UserId);
        var game = await GetGameAsync(gameId);
        var reasons = await GetIndividualEligibilityFailuresAsync(game, userId);
        return new EligibilityResponseDTO(reasons.Count == 0, reasons);
    }

    public async Task<EligibilityResponseDTO> CheckTeamEligibilityAsync(string auth0UserId, Guid gameId, Guid teamId)
    {
        var userId = await GetCurrentUserIdAsync(auth0UserId);
        var game = await GetGameAsync(gameId);
        var team = await GetTeamWithMembersAsync(teamId);
        var reasons = await GetTeamEligibilityFailuresAsync(game, team, userId, null);
        return new EligibilityResponseDTO(reasons.Count == 0, reasons);
    }

    public async Task<RosterCandidateEligibilityResponseDTO> CheckRosterEligibilityAsync(string auth0UserId, Guid gameId, Guid teamId, IReadOnlyList<Guid> userIds)
    {
        var userId = await GetCurrentUserIdAsync(auth0UserId);
        var game = await GetGameAsync(gameId);
        var team = await GetTeamWithMembersAsync(teamId);
        var reasons = await GetTeamEligibilityFailuresAsync(game, team, userId, null);
        reasons.AddRange(GetRosterSizeFailures(game, userIds));

        var distinctUserIds = userIds.Distinct().ToList();
        var users = await GetActiveUserProfilesByIdAsync(distinctUserIds);
        var candidateFailures = await GetRosterCandidateFailuresAsync(game.Id, team, distinctUserIds, null);

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

    public async Task<TournamentRegistrationDTO> RegisterIndividualAsync(string auth0UserId, Guid gameId)
    {
        await using var transaction = await BeginTransactionIfSupportedAsync();
        var userId = await GetCurrentUserIdAsync(auth0UserId);
        var game = await GetGameAsync(gameId);
        var reasons = await GetIndividualEligibilityFailuresAsync(game, userId);
        if (reasons.Count != 0)
            throw new ValidationException(string.Join(", ", reasons));

        var now = DateTime.UtcNow;
        var registration = new TournamentRegistration
        {
            Id = Guid.NewGuid(),
            GameId = game.Id,
            Kind = TournamentRegistrationKind.Individual,
            Status = TournamentRegistrationStatus.Active,
            RegisteredByUserId = userId,
            UserId = userId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.TournamentRegistrations.Add(registration);
        await SaveRegistrationChangesAsync("User already has pending or active participation for this tournament.");
        var dto = new TournamentRegistrationDTO(await GetRegistrationByIdAsync(registration.Id));
        if (transaction is not null)
            await transaction.CommitAsync();
        return dto;
    }

    public async Task UnregisterIndividualAsync(string auth0UserId, Guid gameId)
    {
        await using var transaction = await BeginTransactionIfSupportedAsync();
        var userId = await GetCurrentUserIdAsync(auth0UserId);
        var game = await GetGameAsync(gameId);
        EnsureScheduled(game);

        var registration = await _dbContext.TournamentRegistrations
            .FirstOrDefaultAsync(r =>
                r.GameId == gameId &&
                r.UserId == userId &&
                r.Kind == TournamentRegistrationKind.Individual &&
                r.Status == TournamentRegistrationStatus.Active);
        if (registration is null)
            throw new NotFoundException("Individual registration not found.");

        _dbContext.TournamentRegistrations.Remove(registration);
        await _dbContext.SaveChangesAsync();
        if (transaction is not null)
            await transaction.CommitAsync();
    }

    public async Task<TournamentRegistrationDTO> SubmitTeamRosterAsync(string auth0UserId, Guid gameId, SubmitTeamRosterDTO request)
    {
        await using var transaction = await BeginTransactionIfSupportedAsync();
        var userId = await GetCurrentUserIdAsync(auth0UserId);
        var game = await GetGameAsync(gameId);
        var team = await GetTeamWithMembersAsync(request.TeamId);
        var existing = await _dbContext.TournamentRegistrations
            .Include(registration => registration.RosterMembers)
            .FirstOrDefaultAsync(registration =>
                registration.GameId == gameId &&
                registration.TeamId == team.Id &&
                registration.Kind == TournamentRegistrationKind.Team);
        var excludedRegistrationId = existing?.Id;

        var teamFailures = await GetTeamEligibilityFailuresAsync(game, team, userId, excludedRegistrationId);
        var rosterFailures = GetRosterSizeFailures(game, request.UserIds);
        if (!request.UserIds.Contains(userId))
            rosterFailures.Add("captain_required");
        var candidateFailures = await GetRosterCandidateFailuresAsync(
            game.Id,
            team,
            request.UserIds.Distinct().ToList(),
            excludedRegistrationId);
        foreach (var failure in candidateFailures.Values.SelectMany(static failures => failures))
            rosterFailures.Add(failure);

        var failures = teamFailures.Concat(rosterFailures).Distinct().ToList();
        if (failures.Count != 0)
            throw new ValidationException(string.Join(", ", failures));

        if (existing is not null)
            await DeleteTransientTeamRegistrationAsync(existing);

        var now = DateTime.UtcNow;
        var registration = new TournamentRegistration
        {
            Id = Guid.NewGuid(),
            GameId = game.Id,
            Kind = TournamentRegistrationKind.Team,
            Status = TournamentRegistrationStatus.PendingConfirmation,
            RegisteredByUserId = userId,
            TeamId = team.Id,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        foreach (var memberId in request.UserIds.Distinct())
        {
            var isCaptain = memberId == team.CaptainUserId;
            registration.RosterMembers.Add(new TournamentRegistrationRosterMember
            {
                Id = Guid.NewGuid(),
                GameId = game.Id,
                TeamId = team.Id,
                UserId = memberId,
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
        var rosterConfirmationEvents = CreateRosterConfirmationEvents(registration);

        await SaveRegistrationChangesAsync("One or more roster members already has pending or active participation for this tournament.");
        var dto = new TournamentRegistrationDTO(await GetRegistrationByIdAsync(registration.Id));
        if (transaction is not null)
            await transaction.CommitAsync();
        await PublishRosterConfirmationEventsAsync(rosterConfirmationEvents);
        return dto;
    }

    public async Task<TournamentRegistrationDTO> ConfirmRosterAsync(string auth0UserId, Guid rosterMemberId)
    {
        await using var transaction = await BeginTransactionIfSupportedAsync();
        var userId = await GetCurrentUserIdAsync(auth0UserId);
        var member = await _dbContext.TournamentRegistrationRosterMembers
            .Include(roster => roster.TournamentRegistration)
                .ThenInclude(registration => registration.RosterMembers)
            .Include(roster => roster.TournamentRegistration)
                .ThenInclude(registration => registration.Team)
                    .ThenInclude(team => team!.Members)
            .Include(roster => roster.TournamentRegistration)
                .ThenInclude(registration => registration.Game)
            .FirstOrDefaultAsync(roster => roster.Id == rosterMemberId && roster.UserId == userId);
        if (member is null || member.ConfirmationStatus != RosterMemberConfirmationStatus.Pending)
            throw new NotFoundException("Pending roster confirmation not found.");

        var registration = member.TournamentRegistration;
        EnsureScheduled(registration.Game);
        if (registration.Team is null)
            throw new ValidationException("Team registration is invalid.");

        var candidateFailures = await GetRosterCandidateFailuresAsync(registration.GameId, registration.Team, userId, registration.Id);
        if (candidateFailures.Count != 0)
            throw new ValidationException(string.Join(", ", candidateFailures));

        var now = DateTime.UtcNow;
        member.Confirm(now);
        if (registration.RosterMembers.All(roster => roster.ConfirmationStatus is RosterMemberConfirmationStatus.AutoConfirmed or RosterMemberConfirmationStatus.Confirmed))
            registration.Activate(now);
        else
            registration.UpdatedAtUtc = now;

        await SaveRegistrationChangesAsync("User already has pending or active participation for this tournament.");
        var dto = new TournamentRegistrationDTO(await GetRegistrationByIdAsync(registration.Id));
        if (transaction is not null)
            await transaction.CommitAsync();
        return dto;
    }

    public async Task UnregisterTeamAsync(string auth0UserId, Guid gameId, Guid teamId)
    {
        await using var transaction = await BeginTransactionIfSupportedAsync();
        var userId = await GetCurrentUserIdAsync(auth0UserId);
        var game = await GetGameAsync(gameId);
        EnsureScheduled(game);
        var team = await GetTeamWithMembersAsync(teamId);
        EnsureCaptain(team, userId);

        var registration = await GetTeamRegistrationForMutationAsync(gameId, teamId);
        await DeleteTransientTeamRegistrationAsync(registration);
        await _dbContext.SaveChangesAsync();
        if (transaction is not null)
            await transaction.CommitAsync();
    }

    public async Task<CurrentUserTournamentRegistrationStateDTO> GetCurrentUserStateAsync(string auth0UserId, Guid gameId)
    {
        var userId = await GetCurrentUserIdAsync(auth0UserId);
        var game = await GetGameAsync(gameId);
        var registrations = await GetRegistrationQuery()
            .Where(registration => registration.GameId == gameId)
            .ToListAsync();

        var individual = registrations.FirstOrDefault(registration => registration.UserId == userId);
        var pendingRoster = registrations
            .SelectMany(registration => registration.RosterMembers)
            .FirstOrDefault(member => member.UserId == userId && member.ConfirmationStatus == RosterMemberConfirmationStatus.Pending);
        var activeTeam = registrations.FirstOrDefault(registration =>
            registration.Kind == TournamentRegistrationKind.Team &&
            registration.Status == TournamentRegistrationStatus.Active &&
            registration.RosterMembers.Any(member => member.UserId == userId));
        var captained = registrations.Where(registration => registration.Team?.CaptainUserId == userId).ToList();

        return new CurrentUserTournamentRegistrationStateDTO
        {
            GameId = gameId,
            IndividualRegistration = individual is null ? null : new TournamentRegistrationDTO(individual),
            PendingRosterConfirmation = pendingRoster is null
                ? null
                : new TournamentRosterMemberDTO
                {
                    Id = pendingRoster.Id,
                    User = new PublicUserDTO(pendingRoster.User),
                    IsCaptain = pendingRoster.IsCaptain,
                    ConfirmationStatus = pendingRoster.ConfirmationStatus
                },
            ActiveTeamRegistration = activeTeam is null ? null : new TournamentRegistrationDTO(activeTeam),
            CaptainManagedRegistrations = captained.Select(registration => new TournamentRegistrationDTO(registration)).ToList(),
            CanRegisterIndividual = game.ParticipationMode == ParticipationMode.Individual &&
                                    game.Status == GameStatus.Scheduled &&
                                    individual is null &&
                                    activeTeam is null &&
                                    pendingRoster is null,
            CanConfirmRoster = pendingRoster is not null,
            CanUnregister = individual is not null || activeTeam is not null || captained.Any()
        };
    }

    public async Task<IReadOnlyList<AdminTournamentRegistrationDTO>> GetAdminRegistrationsAsync(Guid gameId)
    {
        _ = await GetGameAsync(gameId);
        var registrations = await GetRegistrationQuery()
            .Where(registration => registration.GameId == gameId)
            .OrderBy(registration => registration.Kind)
            .ThenBy(registration => registration.Status)
            .ThenBy(registration => registration.CreatedAtUtc)
            .ToListAsync();

        return registrations.Select(registration => new AdminTournamentRegistrationDTO(registration)).ToList();
    }

    public async Task RemoveIndividualAsAdminAsync(Guid gameId, Guid userId, string? reason, string? adminAuth0UserId)
    {
        await using var transaction = await BeginTransactionIfSupportedAsync();
        _ = string.IsNullOrWhiteSpace(adminAuth0UserId) ? (Guid?)null : await GetCurrentUserIdAsync(adminAuth0UserId);
        var registration = await _dbContext.TournamentRegistrations.FirstOrDefaultAsync(registration =>
            registration.GameId == gameId &&
            registration.UserId == userId &&
            registration.Kind == TournamentRegistrationKind.Individual);
        if (registration is null)
            throw new NotFoundException("Individual registration not found.");

        _dbContext.TournamentRegistrations.Remove(registration);
        await _dbContext.SaveChangesAsync();
        if (transaction is not null)
            await transaction.CommitAsync();
    }

    public async Task RemoveTeamAsAdminAsync(Guid gameId, Guid teamId, string? reason, string? adminAuth0UserId)
    {
        await using var transaction = await BeginTransactionIfSupportedAsync();
        _ = string.IsNullOrWhiteSpace(adminAuth0UserId) ? (Guid?)null : await GetCurrentUserIdAsync(adminAuth0UserId);
        var registration = await GetTeamRegistrationForMutationAsync(gameId, teamId);
        await DeleteTransientTeamRegistrationAsync(registration);
        await _dbContext.SaveChangesAsync();
        if (transaction is not null)
            await transaction.CommitAsync();
    }

    private async Task<List<string>> GetIndividualEligibilityFailuresAsync(Game game, Guid userId)
    {
        var reasons = new List<string>();
        if (game.ParticipationMode != ParticipationMode.Individual)
            reasons.Add("not_individual_tournament");
        if (game.Status != GameStatus.Scheduled)
            reasons.Add("tournament_not_scheduled");
        if (await HasAnyParticipationAsync(game.Id, userId, null))
            reasons.Add("duplicate_participation");
        return reasons;
    }

    private async Task<List<string>> GetTeamEligibilityFailuresAsync(Game game, Team team, Guid captainUserId, Guid? excludedRegistrationId)
    {
        var reasons = new List<string>();
        if (game.ParticipationMode != ParticipationMode.Team)
            reasons.Add("not_team_tournament");
        if (game.Status != GameStatus.Scheduled)
            reasons.Add("tournament_not_scheduled");
        if (!game.TeamSize.HasValue || game.TeamSize.Value <= 0)
            reasons.Add("team_size_required");
        if (team.IsDeleted)
            reasons.Add("team_deleted");
        if (team.CaptainUserId != captainUserId)
            reasons.Add("captain_required");
        if (await _dbContext.TournamentRegistrations.AnyAsync(registration =>
                registration.GameId == game.Id &&
                registration.TeamId == team.Id &&
                (!excludedRegistrationId.HasValue || registration.Id != excludedRegistrationId.Value)))
            reasons.Add("team_already_registered");
        if (await HasAnyParticipationAsync(game.Id, captainUserId, excludedRegistrationId))
            reasons.Add("captain_duplicate_participation");
        return reasons;
    }

    private static List<string> GetRosterSizeFailures(Game game, IReadOnlyCollection<Guid> userIds)
    {
        var reasons = new List<string>();
        if (game.ParticipationMode == ParticipationMode.Team && game.TeamSize.HasValue && userIds.Distinct().Count() != game.TeamSize.Value)
            reasons.Add("exact_roster_size_required");
        return reasons;
    }

    private async Task<List<string>> GetRosterCandidateFailuresAsync(Guid gameId, Team team, Guid userId, Guid? excludedRegistrationId)
    {
        var failures = await GetRosterCandidateFailuresAsync(gameId, team, [userId], excludedRegistrationId);
        return failures[userId];
    }

    private async Task<Dictionary<Guid, List<string>>> GetRosterCandidateFailuresAsync(
        Guid gameId,
        Team team,
        IReadOnlyCollection<Guid> userIds,
        Guid? excludedRegistrationId)
    {
        var distinctUserIds = userIds.Distinct().ToList();
        if (distinctUserIds.Count == 0)
            return [];

        var activeUserIds = (await GetActiveUserProfilesByIdAsync(distinctUserIds)).Keys.ToHashSet();
        var teamMemberIds = team.Members.Select(member => member.Id).ToHashSet();

        var directParticipationUserIds = await _dbContext.TournamentRegistrations
            .AsNoTracking()
            .Where(registration =>
                registration.GameId == gameId &&
                registration.UserId.HasValue &&
                distinctUserIds.Contains(registration.UserId.Value) &&
                (!excludedRegistrationId.HasValue || registration.Id != excludedRegistrationId.Value))
            .Select(registration => registration.UserId!.Value)
            .ToListAsync();

        var rosterParticipationUserIds = await _dbContext.TournamentRegistrationRosterMembers
            .AsNoTracking()
            .Where(member =>
                member.GameId == gameId &&
                distinctUserIds.Contains(member.UserId) &&
                (!excludedRegistrationId.HasValue || member.TournamentRegistrationId != excludedRegistrationId.Value))
            .Select(member => member.UserId)
            .ToListAsync();
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

    private async Task<bool> HasAnyParticipationAsync(Guid gameId, Guid userId, Guid? excludedRegistrationId)
    {
        return await _dbContext.TournamentRegistrations.AnyAsync(registration =>
                   registration.GameId == gameId &&
                   registration.UserId == userId &&
                   (!excludedRegistrationId.HasValue || registration.Id != excludedRegistrationId.Value))
               || await _dbContext.TournamentRegistrationRosterMembers.AnyAsync(member =>
                   member.GameId == gameId &&
                   member.UserId == userId &&
                   (!excludedRegistrationId.HasValue || member.TournamentRegistrationId != excludedRegistrationId.Value));
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

    private async Task PublishRosterConfirmationEventsAsync(IEnumerable<TournamentRosterConfirmationChangedEvent> rosterConfirmationEvents)
    {
        foreach (var rosterConfirmationEvent in rosterConfirmationEvents)
        {
            await _eventPublisher.RosterConfirmationChangedAsync(
                rosterConfirmationEvent.TeamId,
                rosterConfirmationEvent.RosterMemberId,
                rosterConfirmationEvent.UserId,
                rosterConfirmationEvent.Status);
        }
    }

    private async Task DeleteTransientTeamRegistrationAsync(TournamentRegistration registration)
    {
        _dbContext.TournamentRegistrations.Remove(registration);
        await Task.CompletedTask;
    }

    private async Task<TournamentRegistration> GetTeamRegistrationForMutationAsync(Guid gameId, Guid teamId)
    {
        var registration = await _dbContext.TournamentRegistrations
            .Include(r => r.RosterMembers)
            .FirstOrDefaultAsync(r =>
                r.GameId == gameId &&
                r.TeamId == teamId &&
                r.Kind == TournamentRegistrationKind.Team);
        if (registration is null)
            throw new NotFoundException("Team registration not found.");
        return registration;
    }

    private Task<TournamentRegistration> GetRegistrationByIdAsync(Guid registrationId)
    {
        return GetRegistrationQuery().FirstAsync(registration => registration.Id == registrationId);
    }

    private IQueryable<TournamentRegistration> GetRegistrationQuery()
    {
        return _dbContext.TournamentRegistrations
            .Include(registration => registration.User)
            .Include(registration => registration.Team)
                .ThenInclude(team => team!.Members)
            .Include(registration => registration.RosterMembers)
                .ThenInclude(member => member.User);
    }

    private async Task<Game> GetGameAsync(Guid gameId)
    {
        var game = await _dbContext.Games.FindAsync(gameId);
        if (game is null)
            throw new NotFoundException($"{nameof(Game)} not found");
        return game;
    }

    private async Task<Team> GetTeamWithMembersAsync(Guid teamId)
    {
        var team = await _dbContext.Teams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == teamId);
        if (team is null)
            throw new NotFoundException($"{nameof(Team)} not found");
        return team;
    }

    private async Task<Guid> GetCurrentUserIdAsync(string auth0UserId)
    {
        if (string.IsNullOrWhiteSpace(auth0UserId))
            throw new UnauthorizedAccessException("Authenticated user id is missing.");

        var user = await _identityModule.GetUserProfileByAuth0IdAsync(auth0UserId.Trim());
        if (user is null || user.IsDeleted)
            throw new NotFoundException("Current user profile was not found.");

        return user.Id.Value;
    }

    private async Task<IReadOnlyDictionary<Guid, UserProfileSummary>> GetActiveUserProfilesByIdAsync(IReadOnlyCollection<Guid> userIds)
    {
        var profiles = await _identityModule.GetUsersByIdsAsync(
            userIds.Select(userId => new UserId(userId)).ToArray());

        return profiles.Values
            .Where(user => !user.IsDeleted)
            .ToDictionary(user => user.Id.Value);
    }

    private static void EnsureScheduled(Game game)
    {
        if (game.Status != GameStatus.Scheduled)
            throw new ValidationException("Tournament must be scheduled for registration changes.");
    }

    private static void EnsureCaptain(Team team, Guid userId)
    {
        if (team.CaptainUserId != userId)
            throw new UnauthorizedAccessException("Only the team captain can perform this action.");
    }

    private async Task SaveRegistrationChangesAsync(string duplicateMessage)
    {
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsRegistrationUniqueConstraintViolation(exception))
        {
            throw new ValidationException(duplicateMessage);
        }
    }

    private static bool IsRegistrationUniqueConstraintViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current.Message.Contains("IX_TournamentRegistrations_", StringComparison.OrdinalIgnoreCase) ||
                current.Message.Contains("IX_TournamentRosterMembers_", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<IDbContextTransaction?> BeginTransactionIfSupportedAsync()
    {
        if (_dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
            return null;

        return await _dbContext.Database.BeginTransactionAsync();
    }
}
