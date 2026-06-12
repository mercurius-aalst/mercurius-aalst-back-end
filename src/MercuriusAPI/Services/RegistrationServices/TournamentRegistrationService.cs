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
        var reasons = await GetTeamEligibilityFailuresAsync(game, team, user.Id, null);
        return new EligibilityResponseDTO(reasons.Count == 0, reasons);
    }

    public async Task<RosterCandidateEligibilityResponseDTO> CheckRosterEligibilityAsync(string auth0UserId, Guid gameId, Guid teamId, IReadOnlyList<Guid> userIds)
    {
        var user = await GetCurrentUserAsync(auth0UserId);
        var game = await GetGameAsync(gameId);
        var team = await GetTeamWithMembersAsync(teamId);
        var reasons = await GetTeamEligibilityFailuresAsync(game, team, user.Id, null);
        reasons.AddRange(GetRosterSizeFailures(game, userIds));

        var distinctUserIds = userIds.Distinct().ToList();
        var users = await _dbContext.Users
            .Where(candidate => distinctUserIds.Contains(candidate.Id))
            .ToDictionaryAsync(candidate => candidate.Id);

        var candidateResults = new List<RosterCandidateEligibilityDTO>();
        foreach (var candidateId in distinctUserIds)
        {
            var candidateReasons = await GetRosterCandidateFailuresAsync(game.Id, team, candidateId, null);
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
        var excludedRegistrationId = existing?.Id;

        var teamFailures = await GetTeamEligibilityFailuresAsync(game, team, user.Id, excludedRegistrationId);
        var rosterFailures = GetRosterSizeFailures(game, request.UserIds);
        if (!request.UserIds.Contains(user.Id))
            rosterFailures.Add("captain_required");
        foreach (var candidateId in request.UserIds.Distinct())
            rosterFailures.AddRange(await GetRosterCandidateFailuresAsync(game.Id, team, candidateId, excludedRegistrationId));

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
        var user = await GetCurrentUserAsync(auth0UserId);
        var member = await _dbContext.TournamentRegistrationRosterMembers
            .Include(roster => roster.TournamentRegistration)
                .ThenInclude(registration => registration.RosterMembers)
            .Include(roster => roster.TournamentRegistration)
                .ThenInclude(registration => registration.Team)
                    .ThenInclude(team => team!.Members)
            .Include(roster => roster.TournamentRegistration)
                .ThenInclude(registration => registration.Game)
            .FirstOrDefaultAsync(roster => roster.Id == rosterMemberId && roster.UserId == user.Id);
        if (member is null || member.ConfirmationStatus != RosterMemberConfirmationStatus.Pending)
            throw new NotFoundException("Pending roster confirmation not found.");

        var registration = member.TournamentRegistration;
        EnsureScheduled(registration.Game);
        if (registration.Team is null)
            throw new ValidationException("Team registration is invalid.");

        var candidateFailures = await GetRosterCandidateFailuresAsync(registration.GameId, registration.Team, user.Id, registration.Id);
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
            .FirstOrDefault(member => member.UserId == user.Id && member.ConfirmationStatus == RosterMemberConfirmationStatus.Pending);
        var activeTeam = registrations.FirstOrDefault(registration =>
            registration.Kind == TournamentRegistrationKind.Team &&
            registration.Status == TournamentRegistrationStatus.Active &&
            registration.RosterMembers.Any(member => member.UserId == user.Id));
        var captained = registrations.Where(registration => registration.Team?.CaptainUserId == user.Id).ToList();

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
        var reasons = new List<string>();
        var user = await _dbContext.Users.FindAsync(userId);
        if (user is null || user.IsDeleted)
            reasons.Add("user_not_found");
        if (!team.Members.Any(member => member.Id == userId))
            reasons.Add("not_team_member");
        if (await HasAnyParticipationAsync(gameId, userId, excludedRegistrationId))
            reasons.Add("duplicate_participation");
        return reasons;
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
