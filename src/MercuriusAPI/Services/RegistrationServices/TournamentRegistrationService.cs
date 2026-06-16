using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.DTOs.Auth;
using Mercurius.LAN.API.DTOs.RegistrationDTOs;
using Mercurius.LAN.API.Exceptions;
using Mercurius.LAN.API.Models;
using Mercurius.LAN.API.Services.TeamServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Mercurius.LAN.API.Services.RegistrationServices;

public class TournamentRegistrationService : ITournamentRegistrationService
{
    private const string DeclinedSelectionStatus = "Declined";
    private static readonly Guid[] NoExcludedRegistrationIds = [];
    private readonly MercuriusDBContext _dbContext;
    private readonly ITeamEventPublisher _eventPublisher;

    public TournamentRegistrationService(MercuriusDBContext dbContext, ITeamEventPublisher eventPublisher)
    {
        _dbContext = dbContext;
        _eventPublisher = eventPublisher;
    }

    public async Task<EligibilityResponseDTO> CheckIndividualEligibilityAsync(string auth0UserId, Guid gameId)
    {
        var user = await GetCurrentUserAsync(auth0UserId);
        var game = await GetGameAsync(gameId);
        var reasons = await GetIndividualEligibilityFailuresAsync(game, user.Id);
        return new EligibilityResponseDTO(reasons.Count == 0, reasons);
    }

    public async Task<EligibilityResponseDTO> CheckTeamEligibilityAsync(string auth0UserId, Guid gameId, Guid teamId)
    {
        var user = await GetCurrentUserAsync(auth0UserId);
        var game = await GetGameAsync(gameId);
        var team = await GetTeamWithMembersAsync(teamId);
        var reasons = await GetTeamEligibilityFailuresAsync(game, team, user.Id, NoExcludedRegistrationIds);
        return new EligibilityResponseDTO(reasons.Count == 0, reasons);
    }

    public async Task<RosterCandidateEligibilityResponseDTO> CheckRosterEligibilityAsync(string auth0UserId, Guid gameId, Guid teamId, IReadOnlyList<Guid> userIds)
    {
        var user = await GetCurrentUserAsync(auth0UserId);
        var game = await GetGameAsync(gameId);
        var team = await GetTeamWithMembersAsync(teamId);
        var reasons = await GetTeamEligibilityFailuresAsync(game, team, user.Id, NoExcludedRegistrationIds);
        reasons.AddRange(GetRosterSizeFailures(game, userIds));

        var distinctUserIds = userIds.Distinct().ToList();
        var users = await _dbContext.Users
            .Where(candidate => distinctUserIds.Contains(candidate.Id))
            .ToDictionaryAsync(candidate => candidate.Id);

        var candidateResults = new List<RosterCandidateEligibilityDTO>();
        foreach (var candidateId in distinctUserIds)
        {
            var candidateReasons = await GetRosterCandidateFailuresAsync(game.Id, team, candidateId, NoExcludedRegistrationIds);
            candidateResults.Add(new RosterCandidateEligibilityDTO(
                candidateId,
                users.TryGetValue(candidateId, out var candidate) ? new PublicUserDTO(candidate) : null,
                candidateReasons.Count == 0,
                candidateReasons));
        }

        if (candidateResults.Any(candidate => !candidate.Eligible))
            reasons.Add("roster_candidate_ineligible");

        return new RosterCandidateEligibilityResponseDTO(reasons.Count == 0, reasons.Distinct().ToList(), candidateResults);
    }

    public async Task<TournamentRegistrationDTO> RegisterIndividualAsync(string auth0UserId, Guid gameId)
    {
        await using var transaction = await BeginTransactionIfSupportedAsync();
        var user = await GetCurrentUserAsync(auth0UserId);
        var game = await GetGameAsync(gameId);
        var reasons = await GetIndividualEligibilityFailuresAsync(game, user.Id);
        if (reasons.Count != 0)
            throw new ValidationException(string.Join(", ", reasons));

        var now = DateTime.UtcNow;
        var registration = new TournamentRegistration
        {
            Id = Guid.NewGuid(),
            GameId = game.Id,
            Kind = TournamentRegistrationKind.Individual,
            Status = TournamentRegistrationStatus.Active,
            RegisteredByUserId = user.Id,
            UserId = user.Id,
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
        var user = await GetCurrentUserAsync(auth0UserId);
        var game = await GetGameAsync(gameId);
        EnsureScheduled(game);

        var registration = await _dbContext.TournamentRegistrations
            .FirstOrDefaultAsync(r =>
                r.GameId == gameId &&
                r.UserId == user.Id &&
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
        var user = await GetCurrentUserAsync(auth0UserId);
        var game = await GetGameAsync(gameId);
        var team = await GetTeamWithMembersAsync(request.TeamId);
        var existing = await _dbContext.TournamentRegistrations
            .Include(registration => registration.RosterMembers)
            .FirstOrDefaultAsync(registration =>
                registration.GameId == gameId &&
                registration.TeamId == team.Id &&
                registration.Kind == TournamentRegistrationKind.Team);
        var pendingSelectionsToDecline = await GetPendingSelectionRegistrationsForUserAsync(game.Id, user.Id, existing?.Id);
        var excludedRegistrationIds = pendingSelectionsToDecline
            .Select(registration => registration.Id)
            .ToList();
        if (existing is not null)
            excludedRegistrationIds.Add(existing.Id);

        var teamFailures = await GetTeamEligibilityFailuresAsync(game, team, user.Id, excludedRegistrationIds);
        var rosterFailures = GetRosterSizeFailures(game, request.UserIds);
        if (!request.UserIds.Contains(user.Id))
            rosterFailures.Add("captain_required");
        foreach (var candidateId in request.UserIds.Distinct())
            rosterFailures.AddRange(await GetRosterCandidateFailuresAsync(game.Id, team, candidateId, excludedRegistrationIds));

        var failures = teamFailures.Concat(rosterFailures).Distinct().ToList();
        if (failures.Count != 0)
            throw new ValidationException(string.Join(", ", failures));

        if (existing is not null)
            await DeleteTransientTeamRegistrationAsync(existing);
        var declinedSelectionEvents = CreatePendingSelectionEvents(pendingSelectionsToDecline, DeclinedSelectionStatus);
        foreach (var pendingSelectionRegistration in pendingSelectionsToDecline)
            await DeleteTransientTeamRegistrationAsync(pendingSelectionRegistration);

        var now = DateTime.UtcNow;
        var registration = new TournamentRegistration
        {
            Id = Guid.NewGuid(),
            GameId = game.Id,
            Kind = TournamentRegistrationKind.Team,
            Status = TournamentRegistrationStatus.PendingConfirmation,
            RegisteredByUserId = user.Id,
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
                SelectionStatus = isCaptain ? RosterSelectionStatus.AutoConfirmed : RosterSelectionStatus.Pending,
                ConfirmedAtUtc = isCaptain ? now : null,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        if (registration.RosterMembers.All(member => member.SelectionStatus != RosterSelectionStatus.Pending))
            registration.Activate(now);

        _dbContext.TournamentRegistrations.Add(registration);
        var rosterSelectionEvents = CreateRosterSelectionEvents(registration);

        await SaveRegistrationChangesAsync("One or more roster members already has pending or active participation for this tournament.");
        var dto = new TournamentRegistrationDTO(await GetRegistrationByIdAsync(registration.Id));
        if (transaction is not null)
            await transaction.CommitAsync();
        await PublishRosterSelectionEventsAsync(declinedSelectionEvents);
        await PublishRosterSelectionEventsAsync(rosterSelectionEvents);
        return dto;
    }

    public async Task<TournamentRegistrationDTO?> RespondToRosterSelectionAsync(string auth0UserId, Guid teamId, Guid rosterMemberId, RosterSelectionActionDTO request)
    {
        if (!Enum.IsDefined(request.Action))
            throw new ValidationException("Unsupported roster selection action.");

        return request.Action switch
        {
            RosterSelectionAction.Confirm => await ConfirmRosterSelectionAsync(auth0UserId, teamId, rosterMemberId),
            RosterSelectionAction.Decline => await DeclineRosterSelectionAsync(auth0UserId, teamId, rosterMemberId),
            _ => throw new ValidationException("Unsupported roster selection action.")
        };
    }

    private async Task<TournamentRegistrationDTO> ConfirmRosterSelectionAsync(string auth0UserId, Guid teamId, Guid rosterMemberId)
    {
        await using var transaction = await BeginTransactionIfSupportedAsync();
        var user = await GetCurrentUserAsync(auth0UserId);
        var member = await _dbContext.TournamentRegistrationRosterMembers
            .Include(roster => roster.TournamentRegistration)
                .ThenInclude(registration => registration.RosterMembers)
            .Include(roster => roster.TournamentRegistration)
                .ThenInclude(registration => registration.Team)
                    .ThenInclude(team => team!.Members)
            .Include(roster => roster.TournamentRegistration)
                .ThenInclude(registration => registration.Game)
            .FirstOrDefaultAsync(roster => roster.Id == rosterMemberId && roster.TeamId == teamId && roster.UserId == user.Id);
        if (member is null || member.SelectionStatus != RosterSelectionStatus.Pending)
            throw new NotFoundException("Pending roster selection not found.");

        var registration = member.TournamentRegistration;
        EnsureScheduled(registration.Game);
        if (registration.Team is null)
            throw new ValidationException("Team registration is invalid.");

        var candidateFailures = await GetRosterCandidateFailuresAsync(registration.GameId, registration.Team, user.Id, [registration.Id]);
        if (candidateFailures.Count != 0)
            throw new ValidationException(string.Join(", ", candidateFailures));

        var now = DateTime.UtcNow;
        member.Confirm(now);
        if (registration.RosterMembers.All(roster => roster.SelectionStatus is RosterSelectionStatus.AutoConfirmed or RosterSelectionStatus.Confirmed))
            registration.Activate(now);
        else
            registration.UpdatedAtUtc = now;

        await SaveRegistrationChangesAsync("User already has pending or active participation for this tournament.");
        var dto = new TournamentRegistrationDTO(await GetRegistrationByIdAsync(registration.Id));
        if (transaction is not null)
            await transaction.CommitAsync();
        return dto;
    }

    private async Task<TournamentRegistrationDTO?> DeclineRosterSelectionAsync(string auth0UserId, Guid teamId, Guid rosterMemberId)
    {
        await using var transaction = await BeginTransactionIfSupportedAsync();
        var user = await GetCurrentUserAsync(auth0UserId);
        var member = await _dbContext.TournamentRegistrationRosterMembers
            .Include(roster => roster.TournamentRegistration)
                .ThenInclude(registration => registration.RosterMembers)
            .Include(roster => roster.TournamentRegistration)
                .ThenInclude(registration => registration.Game)
            .FirstOrDefaultAsync(roster => roster.Id == rosterMemberId && roster.TeamId == teamId && roster.UserId == user.Id);
        if (member is null || member.SelectionStatus != RosterSelectionStatus.Pending)
            throw new NotFoundException("Pending roster selection not found.");

        var registration = member.TournamentRegistration;
        EnsureScheduled(registration.Game);

        var declinedSelectionEvents = CreatePendingSelectionEvents([registration], DeclinedSelectionStatus);
        await DeleteTransientTeamRegistrationAsync(registration);
        await _dbContext.SaveChangesAsync();
        if (transaction is not null)
            await transaction.CommitAsync();
        await PublishRosterSelectionEventsAsync(declinedSelectionEvents);
        return null;
    }

    public async Task UnregisterTeamAsync(string auth0UserId, Guid gameId, Guid teamId)
    {
        await using var transaction = await BeginTransactionIfSupportedAsync();
        var user = await GetCurrentUserAsync(auth0UserId);
        var game = await GetGameAsync(gameId);
        EnsureScheduled(game);
        var team = await GetTeamWithMembersAsync(teamId);
        EnsureCaptain(team, user.Id);

        var registration = await GetTeamRegistrationForMutationAsync(gameId, teamId);
        await DeleteTransientTeamRegistrationAsync(registration);
        await _dbContext.SaveChangesAsync();
        if (transaction is not null)
            await transaction.CommitAsync();
    }

    public async Task<CurrentUserTournamentRegistrationStateDTO> GetCurrentUserStateAsync(string auth0UserId, Guid gameId)
    {
        var user = await GetCurrentUserAsync(auth0UserId);
        var game = await GetGameAsync(gameId);
        var registrations = await GetRegistrationQuery()
            .Where(registration => registration.GameId == gameId)
            .ToListAsync();

        var individual = registrations.FirstOrDefault(registration => registration.UserId == user.Id);
        var pendingRoster = registrations
            .SelectMany(registration => registration.RosterMembers)
            .FirstOrDefault(member => member.UserId == user.Id && member.SelectionStatus == RosterSelectionStatus.Pending);
        var activeTeam = registrations.FirstOrDefault(registration =>
            registration.Kind == TournamentRegistrationKind.Team &&
            registration.Status == TournamentRegistrationStatus.Active &&
            registration.RosterMembers.Any(member => member.UserId == user.Id));
        var captained = registrations.Where(registration => registration.Team?.CaptainUserId == user.Id).ToList();

        return new CurrentUserTournamentRegistrationStateDTO
        {
            GameId = gameId,
            IndividualRegistration = individual is null ? null : new TournamentRegistrationDTO(individual),
            PendingRosterSelection = pendingRoster is null
                ? null
                : new TournamentRosterMemberDTO
                {
                    Id = pendingRoster.Id,
                    User = new PublicUserDTO(pendingRoster.User),
                    IsCaptain = pendingRoster.IsCaptain,
                    SelectionStatus = pendingRoster.SelectionStatus
                },
            ActiveTeamRegistration = activeTeam is null ? null : new TournamentRegistrationDTO(activeTeam),
            CaptainManagedRegistrations = captained.Select(registration => new TournamentRegistrationDTO(registration)).ToList(),
            CanRegisterIndividual = game.ParticipationMode == ParticipationMode.Individual &&
                                    game.Status == GameStatus.Scheduled &&
                                    individual is null &&
                                    activeTeam is null &&
                                    pendingRoster is null,
            CanRespondToRosterSelection = pendingRoster is not null,
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
        var admin = string.IsNullOrWhiteSpace(adminAuth0UserId) ? null : await GetCurrentUserAsync(adminAuth0UserId);
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
        var admin = string.IsNullOrWhiteSpace(adminAuth0UserId) ? null : await GetCurrentUserAsync(adminAuth0UserId);
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
        if (await HasAnyParticipationAsync(game.Id, userId, NoExcludedRegistrationIds))
            reasons.Add("duplicate_participation");
        return reasons;
    }

    private async Task<List<string>> GetTeamEligibilityFailuresAsync(Game game, Team team, Guid captainUserId, IReadOnlyCollection<Guid> excludedRegistrationIds)
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
                (excludedRegistrationIds.Count == 0 || !excludedRegistrationIds.Contains(registration.Id))))
            reasons.Add("team_already_registered");
        if (await HasAnyParticipationAsync(game.Id, captainUserId, excludedRegistrationIds))
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

    private async Task<List<string>> GetRosterCandidateFailuresAsync(Guid gameId, Team team, Guid userId, IReadOnlyCollection<Guid> excludedRegistrationIds)
    {
        var reasons = new List<string>();
        var user = await _dbContext.Users.FindAsync(userId);
        if (user is null || user.IsDeleted)
            reasons.Add("user_not_found");
        if (!team.Members.Any(member => member.Id == userId))
            reasons.Add("not_team_member");
        if (await HasAnyParticipationAsync(gameId, userId, excludedRegistrationIds))
            reasons.Add("duplicate_participation");
        return reasons;
    }

    private async Task<bool> HasAnyParticipationAsync(Guid gameId, Guid userId, IReadOnlyCollection<Guid> excludedRegistrationIds)
    {
        return await _dbContext.TournamentRegistrations.AnyAsync(registration =>
                   registration.GameId == gameId &&
                   registration.UserId == userId &&
                   (excludedRegistrationIds.Count == 0 || !excludedRegistrationIds.Contains(registration.Id)))
               || await _dbContext.TournamentRegistrationRosterMembers.AnyAsync(member =>
                   member.GameId == gameId &&
                   member.UserId == userId &&
                   (excludedRegistrationIds.Count == 0 || !excludedRegistrationIds.Contains(member.TournamentRegistrationId)));
    }

    private static List<TournamentRosterSelectionChangedEvent> CreateRosterSelectionEvents(
     TournamentRegistration registration)
    {
        return registration.RosterMembers
            .Where(member => member.SelectionStatus == RosterSelectionStatus.Pending)
            .Select(member => new TournamentRosterSelectionChangedEvent(
                registration.TeamId!.Value,
                member.Id,
                member.UserId,
                nameof(RosterSelectionStatus.Pending)))
            .ToList();
    }

    private static List<TournamentRosterSelectionChangedEvent> CreatePendingSelectionEvents(
        IEnumerable<TournamentRegistration> registrations,
        string status)
    {
        return registrations
            .Where(registration => registration.TeamId.HasValue)
            .SelectMany(registration => registration.RosterMembers
                .Where(member => member.SelectionStatus == RosterSelectionStatus.Pending)
                .Select(member => new TournamentRosterSelectionChangedEvent(
                    registration.TeamId!.Value,
                    member.Id,
                    member.UserId,
                    status)))
            .ToList();
    }

    private async Task<List<TournamentRegistration>> GetPendingSelectionRegistrationsForUserAsync(
        Guid gameId,
        Guid userId,
        Guid? excludedRegistrationId)
    {
        var registrationIds = await _dbContext.TournamentRegistrationRosterMembers
            .Where(member =>
                member.GameId == gameId &&
                member.UserId == userId &&
                member.SelectionStatus == RosterSelectionStatus.Pending &&
                member.TournamentRegistration.Status == TournamentRegistrationStatus.PendingConfirmation &&
                (!excludedRegistrationId.HasValue || member.TournamentRegistrationId != excludedRegistrationId.Value))
            .Select(member => member.TournamentRegistrationId)
            .Distinct()
            .ToListAsync();

        if (registrationIds.Count == 0)
            return [];

        return await _dbContext.TournamentRegistrations
            .Include(registration => registration.RosterMembers)
            .Where(registration => registrationIds.Contains(registration.Id))
            .ToListAsync();
    }

    private async Task PublishRosterSelectionEventsAsync(IEnumerable<TournamentRosterSelectionChangedEvent> rosterSelectionEvents)
    {
        foreach (var rosterSelectionEvent in rosterSelectionEvents)
        {
            await _eventPublisher.RosterSelectionChangedAsync(
                rosterSelectionEvent.TeamId,
                rosterSelectionEvent.RosterMemberId,
                rosterSelectionEvent.UserId,
                rosterSelectionEvent.Status);
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

    private async Task<User> GetCurrentUserAsync(string auth0UserId)
    {
        if (string.IsNullOrWhiteSpace(auth0UserId))
            throw new UnauthorizedAccessException("Authenticated user id is missing.");

        var normalizedAuth0UserId = auth0UserId.Trim();
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Auth0UserId == normalizedAuth0UserId && !u.IsDeleted);
        if (user is null)
            throw new NotFoundException("Current user profile was not found.");

        return user;
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
