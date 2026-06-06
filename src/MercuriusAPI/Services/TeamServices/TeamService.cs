using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.DTOs.TeamDTOs;
using Mercurius.LAN.API.Exceptions;
using Mercurius.LAN.API.Models;
using Mercurius.LAN.API.Services.Files;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.LAN.API.Services.TeamServices;

public class TeamService : ITeamService
{
    private const int MaxCaptainedTeams = 2;
    private const int MaxTeamSearchResults = 25;
    private readonly MercuriusDBContext _dbContext;
    private readonly IFileService? _fileService;
    private readonly ITeamEventPublisher _eventPublisher;
    private readonly int _inviteResendCooldownDays;
    private readonly int _inviteExpirationDays;
    private readonly int _inviteRetentionDays;
    private readonly int _declinedInviteResendLimit;

    public TeamService(
        MercuriusDBContext dbContext,
        IConfiguration configuration,
        IFileService? fileService = null,
        ITeamEventPublisher? eventPublisher = null)
    {
        _dbContext = dbContext;
        _fileService = fileService;
        _eventPublisher = eventPublisher ?? new NullTeamEventPublisher();
        _inviteResendCooldownDays = configuration.GetSection("TeamInvite:ResendCooldownDays").Get<int>();
        _inviteExpirationDays = configuration.GetSection("TeamInvite:ExpirationDays").Get<int?>() ?? 14;
        _inviteRetentionDays = configuration.GetSection("TeamInvite:RetentionDays").Get<int?>() ?? 90;
        _declinedInviteResendLimit = configuration.GetSection("TeamInvite:DeclinedResendLimit").Get<int?>() ?? 3;
    }

    public async Task<GetTeamDTO> CreateTeamAsync(CreateTeamDTO teamDTO)
    {
        var normalizedTeamName = Team.NormalizeName(teamDTO.Name);
        if (await CheckIfTeamNameExistsAsync(normalizedTeamName))
            throw new ValidationException($"Teamname {teamDTO.Name} already in use");
        var captain = await _dbContext.Users.FindAsync(teamDTO.CaptainUserId);
        if (captain is null)
            throw new NotFoundException($"{nameof(User)} not found");
        var team = new Team(teamDTO.Name, captain);
        _dbContext.Teams.Add(team);
        await SaveTeamChangesAsync(teamDTO.Name);
        return new GetTeamDTO(team);
    }

    public async Task<TeamManagementSummaryDTO> CreateCurrentUserTeamAsync(string auth0UserId, CreateTeamDTO teamDTO)
    {
        var currentUser = await GetCurrentUserAsync(auth0UserId);
        await EnsureCaptainLimitAsync(currentUser.Id);

        var normalizedTeamName = Team.NormalizeName(teamDTO.Name);
        if (await CheckIfTeamNameExistsAsync(normalizedTeamName))
            throw new ValidationException($"Teamname {teamDTO.Name} already in use");

        var team = new Team(teamDTO.Name, currentUser) { Id = Guid.NewGuid() };
        _dbContext.Teams.Add(team);
        await SaveTeamChangesAsync(teamDTO.Name);

        return new TeamManagementSummaryDTO(team);
    }
    public async Task DeleteTeamAsync(Guid teamId)
    {
        var team = await _dbContext.Teams.FindAsync(teamId);
        if (team is null)
            throw new NotFoundException($"{nameof(Team)} not found");
        team.Delete();
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteTeamAsync(string auth0UserId, Guid teamId)
    {
        var currentUser = await GetCurrentUserAsync(auth0UserId);
        var team = await GetActiveTeamsQuery().FirstOrDefaultAsync(t => t.Id == teamId);
        if (team is null)
            throw new NotFoundException($"{nameof(Team)} not found");

        EnsureCaptain(team, currentUser.Id);
        if (await IsTeamInDeleteBlockingTournamentAsync(teamId))
            throw new ValidationException("Cannot delete a team that is actively participating in a game or tournament.");

        team.Delete();
        await _dbContext.SaveChangesAsync();
    }

    public IEnumerable<GetTeamDTO> GetAllTeams()
    {
        return GetTeamWithMembersQuery()
            .Select(t => new GetTeamDTO(t));
    }
    public async Task<Team> GetTeamByIdAsync(Guid teamId)
    {
        var team = await GetTeamDetailsQuery()
            .FirstOrDefaultAsync(t => t.Id == teamId);
        if (team is null)
            throw new NotFoundException($"{nameof(Team)} not found");
        return team;
    }

    public async Task<PublicTeamProfileDTO> GetPublicTeamProfileAsync(string teamName)
    {
        var normalizedTeamName = Team.NormalizeName(teamName);
        var team = await _dbContext.Teams
            .AsNoTracking()
            .Where(t => !t.IsDeleted)
            .Include(t => t.Captain)
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.NormalizedName == normalizedTeamName);

        if (team is null)
            throw new NotFoundException($"{nameof(Team)} not found");

        var tournaments = await _dbContext.Games
            .AsNoTracking()
            .Where(game => game.RegisteredTeams.Any(registeredTeam => registeredTeam.Id == team.Id))
            .Select(game => new PublicTeamTournamentDTO
            {
                GameId = game.Id,
                Name = game.Name
            })
            .OrderBy(game => game.Name)
            .ThenBy(game => game.GameId)
            .ToListAsync();

        var members = team.Members
            .Select(member => member.Username)
            .Where(IsValidPublicUsername)
            .Select(username => username!)
            .OrderBy(username => username, StringComparer.OrdinalIgnoreCase)
            .ThenBy(username => username, StringComparer.Ordinal)
            .Select(username => new PublicTeamMemberDTO(username))
            .ToList();

        return new PublicTeamProfileDTO
        {
            TeamName = team.Name,
            CaptainUsername = IsValidPublicUsername(team.Captain.Username) ? team.Captain.Username : null,
            LogoUrl = team.LogoUrl,
            Members = members,
            Tournaments = tournaments
        };
    }

    public async Task<Team> GetTeamByNameAsync(string name)
    {
        var normalizedName = Team.NormalizeName(name);
        var team = await GetTeamWithMembersQuery()
            .FirstOrDefaultAsync(t => t.NormalizedName == normalizedName);
        if (team is null)
            throw new NotFoundException($"{nameof(Team)} not found");
        return team;
    }

    public async Task<GetTeamDTO> RemoveMemberAsync(Guid id, Guid userId)
    {
        var team = await _dbContext.Teams.Include(t => t.Members).FirstOrDefaultAsync(t => t.Id == id);
        if (team is null)
            throw new NotFoundException($"{nameof(Team)} not found");
        team.RemoveMember(userId);
        await _dbContext.SaveChangesAsync();
        return new GetTeamDTO(team);
    }

    public async Task<TeamManagementSummaryDTO> RemoveMemberAsync(string auth0UserId, Guid teamId, Guid userId)
    {
        var currentUser = await GetCurrentUserAsync(auth0UserId);
        var team = await GetTeamWithMembersQuery().FirstOrDefaultAsync(t => t.Id == teamId);
        if (team is null)
            throw new NotFoundException($"{nameof(Team)} not found");

        EnsureCaptain(team, currentUser.Id);
        if (await IsTeamInMemberRemovalBlockingTournamentAsync(teamId))
            throw new ValidationException("Cannot remove a member from a team that is part of an in-progress tournament roster.");

        team.RemoveMember(userId);
        await _dbContext.SaveChangesAsync();
        await _eventPublisher.MembershipChangedAsync(team.Id, userId, "Removed");
        return new TeamManagementSummaryDTO(team);
    }

    public async Task<GetTeamDTO> UpdateTeamAsync(Guid id, UpdateTeamDTO teamDTO)
    {
        var team = await GetTeamWithMembersQuery()
            .FirstOrDefaultAsync(t => t.Id == id);
        if (team is null)
            throw new NotFoundException($"{nameof(Team)} not found");

        if (teamDTO.Name != null)
        {
            var normalizedTeamName = Team.NormalizeName(teamDTO.Name);
            if (!string.Equals(team.NormalizedName, normalizedTeamName, StringComparison.Ordinal) &&
                await CheckIfTeamNameExistsAsync(normalizedTeamName, id))
            {
                throw new ValidationException($"Teamname {teamDTO.Name} already in use");
            }

            team.UpdateName(teamDTO.Name);
        }

        if (teamDTO.CaptainUserId.HasValue)
            team.ChangeCaptain(teamDTO.CaptainUserId.Value);

        _dbContext.Teams.Update(team);
        await SaveTeamChangesAsync(team.Name);
        return new GetTeamDTO(team);
    }

    public async Task<IEnumerable<GetTeamDTO>> SearchTeamsByNameAsync(string query, int? limit = null)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var normalizedQuery = query.Trim().ToLowerInvariant();
        var resultLimit = Math.Clamp(limit.GetValueOrDefault(MaxTeamSearchResults), 1, MaxTeamSearchResults);

        return await GetTeamWithMembersQuery()
            .Where(t => t.NormalizedName.StartsWith(normalizedQuery))
            .OrderBy(t => t.Name)
            .Take(resultLimit)
            .Select(t => new GetTeamDTO(t))
            .ToListAsync();
    }

    public async Task<TeamInviteDTO> InviteUserAsync(Guid teamId, Guid userId)
    {
        var team = await GetTeamByIdAsync(teamId);
        if (!await _dbContext.Users.AnyAsync(u => u.Id == userId))
            throw new NotFoundException($"{nameof(User)} not found");
        await ExpirePendingInvitesAsync(teamId, userId);
        var invite = team.InviteUser(userId, _inviteResendCooldownDays, _inviteExpirationDays, _declinedInviteResendLimit);
        await _dbContext.SaveChangesAsync();
        return new TeamInviteDTO(invite);
    }

    public async Task<TeamInviteDTO> InviteUserAsync(string auth0UserId, Guid teamId, Guid userId)
    {
        var currentUser = await GetCurrentUserAsync(auth0UserId);
        var team = await GetTeamByIdAsync(teamId);
        EnsureCaptain(team, currentUser.Id);

        if (!await _dbContext.Users.AnyAsync(u => u.Id == userId && !u.IsDeleted))
            throw new NotFoundException($"{nameof(User)} not found");

        await ExpirePendingInvitesAsync(teamId, userId);
        var invite = team.InviteUser(userId, _inviteResendCooldownDays, _inviteExpirationDays, _declinedInviteResendLimit);
        await SaveInviteChangesAsync();
        await _eventPublisher.InviteChangedAsync(teamId, invite.Id, userId, invite.Status.ToString());
        return new TeamInviteDTO(invite);
    }

    public async Task<TeamInviteDTO> CancelInviteAsync(string auth0UserId, Guid teamId, Guid inviteId)
    {
        var currentUser = await GetCurrentUserAsync(auth0UserId);
        var invite = await _dbContext.TeamInvites
            .Include(i => i.Team)
            .FirstOrDefaultAsync(i => i.Id == inviteId && i.TeamId == teamId);
        if (invite is null)
            throw new NotFoundException("Invite not found");

        EnsureCaptain(invite.Team, currentUser.Id);
        invite.Cancel();
        await _dbContext.SaveChangesAsync();
        await _eventPublisher.InviteChangedAsync(teamId, invite.Id, invite.UserId, invite.Status.ToString());
        return new TeamInviteDTO(invite);
    }

    public async Task<TeamInviteDTO> RespondToInviteAsync(Guid teamId, Guid userId, bool accept)
    {
        var invite = await _dbContext.TeamInvites
            .Include(ti => ti.User)
            .Include(ti => ti.Team)
            .FirstOrDefaultAsync(i => i.TeamId == teamId && i.UserId == userId && i.Status == TeamInviteStatus.Pending);
        if (invite == null)
            throw new NotFoundException("No pending invite found");
        await _dbContext.Entry(invite.Team).Collection(t => t.Members).LoadAsync();
        invite.Respond(accept);
        await _dbContext.SaveChangesAsync();
        return new TeamInviteDTO(invite);
    }

    public async Task<TeamInviteDTO> RespondToInviteAsync(string auth0UserId, Guid inviteId, bool accept)
    {
        var currentUser = await GetCurrentUserAsync(auth0UserId);
        var invite = await _dbContext.TeamInvites
            .Include(ti => ti.User)
            .Include(ti => ti.Team)
            .FirstOrDefaultAsync(i => i.Id == inviteId && i.UserId == currentUser.Id);
        if (invite == null)
            throw new NotFoundException("No pending invite found");

        await _dbContext.Entry(invite.Team).Collection(t => t.Members).LoadAsync();
        invite.Respond(accept);
        await _dbContext.SaveChangesAsync();
        await _eventPublisher.InviteChangedAsync(invite.TeamId, invite.Id, invite.UserId, invite.Status.ToString());
        if (accept)
            await _eventPublisher.MembershipChangedAsync(invite.TeamId, invite.UserId, "Joined");

        return new TeamInviteDTO(invite);
    }

    public async Task<IEnumerable<TeamInviteDTO>> GetUserInvitesAsync(Guid userId)
    {
        var invites = await _dbContext.TeamInvites
            .Where(i => i.UserId == userId && i.Status == TeamInviteStatus.Pending)
            .ToListAsync();
        return invites.Select(invite => new TeamInviteDTO
        {
            Id = invite.Id,
            TeamId = invite.TeamId,
            UserId = invite.UserId,
            Status = invite.Status.ToString(),
            CreatedAt = invite.CreatedAt,
            ExpiresAt = invite.ExpiresAt
        });
    }

    public async Task<CurrentUserTeamSummaryDTO> GetCurrentUserTeamSummaryAsync(string auth0UserId)
    {
        var currentUser = await GetCurrentUserAsync(auth0UserId);
        await ExpirePendingInvitesAsync();
        await CleanupTerminalInvitesAsync();

        var captainedTeams = await TeamManagementQuery()
            .Where(team => team.CaptainUserId == currentUser.Id)
            .OrderBy(team => team.Name)
            .ToListAsync();

        var memberTeams = await TeamManagementQuery()
            .Where(team => team.Members.Any(member => member.Id == currentUser.Id))
            .OrderBy(team => team.Name)
            .ToListAsync();

        return new CurrentUserTeamSummaryDTO
        {
            CaptainedTeams = captainedTeams.Select(team => new TeamManagementSummaryDTO(team)),
            MemberTeams = memberTeams.Select(team => new TeamManagementSummaryDTO(team)),
            ReceivedPendingInvites = (await GetPendingInviteSummariesQuery()
                .Where(invite => invite.UserId == currentUser.Id)
                .OrderBy(invite => invite.CreatedAt)
                .ToListAsync())
                .Select(invite => new TeamInviteSummaryDTO(invite)),
            SentPendingInvites = (await GetPendingInviteSummariesQuery()
                .Where(invite => invite.Team.CaptainUserId == currentUser.Id)
                .OrderBy(invite => invite.CreatedAt)
                .ToListAsync())
                .Select(invite => new TeamInviteSummaryDTO(invite))
        };
    }

    public async Task<IEnumerable<TeamInviteSummaryDTO>> GetCurrentUserInvitesAsync(string auth0UserId)
    {
        var currentUser = await GetCurrentUserAsync(auth0UserId);
        await ExpirePendingInvitesAsync();
        var invites = await GetPendingInviteSummariesQuery()
            .Where(invite => invite.UserId == currentUser.Id)
            .OrderBy(invite => invite.CreatedAt)
            .ToListAsync();
        return invites.Select(invite => new TeamInviteSummaryDTO(invite));
    }

    public async Task<IEnumerable<TeamInviteSummaryDTO>> GetCurrentUserSentInvitesAsync(string auth0UserId)
    {
        var currentUser = await GetCurrentUserAsync(auth0UserId);
        await ExpirePendingInvitesAsync();
        var invites = await GetPendingInviteSummariesQuery()
            .Where(invite => invite.Team.CaptainUserId == currentUser.Id)
            .OrderBy(invite => invite.CreatedAt)
            .ToListAsync();
        return invites.Select(invite => new TeamInviteSummaryDTO(invite));
    }

    public async Task<TeamManagementSummaryDTO> LeaveTeamAsync(string auth0UserId, Guid teamId)
    {
        var currentUser = await GetCurrentUserAsync(auth0UserId);
        var team = await GetTeamWithMembersQuery().FirstOrDefaultAsync(t => t.Id == teamId);
        if (team is null)
            throw new NotFoundException($"{nameof(Team)} not found");
        if (team.CaptainUserId == currentUser.Id)
            throw new ValidationException("The captain cannot leave a team without transferring captainship.");
        if (!team.Members.Any(member => member.Id == currentUser.Id))
            throw new NotFoundException($"{nameof(User)} not found in {team.Name}");
        if (await IsTeamInLeaveBlockingTournamentAsync(teamId))
            throw new ValidationException("Cannot leave a team that is part of a protected tournament roster.");

        team.RemoveMember(currentUser.Id);
        await _dbContext.SaveChangesAsync();
        await _eventPublisher.MembershipChangedAsync(team.Id, currentUser.Id, "Left");
        return new TeamManagementSummaryDTO(team);
    }

    public async Task<TeamManagementSummaryDTO> TransferCaptainAsync(string auth0UserId, Guid teamId, Guid newCaptainUserId)
    {
        var currentUser = await GetCurrentUserAsync(auth0UserId);
        var team = await GetTeamWithMembersQuery()
            .Include(t => t.Captain)
            .FirstOrDefaultAsync(t => t.Id == teamId);
        if (team is null)
            throw new NotFoundException($"{nameof(Team)} not found");

        EnsureCaptain(team, currentUser.Id);
        await EnsureCaptainLimitAsync(newCaptainUserId, teamId);
        team.ChangeCaptain(newCaptainUserId);
        await _dbContext.SaveChangesAsync();
        await _eventPublisher.CaptainTransferredAsync(team.Id, newCaptainUserId);
        return new TeamManagementSummaryDTO(team);
    }

    public async Task<TeamLogoResponseDTO> UploadTeamLogoAsync(string auth0UserId, Guid teamId, IFormFile logo)
    {
        if (_fileService is null)
            throw new InvalidOperationException("File service is not configured.");

        var currentUser = await GetCurrentUserAsync(auth0UserId);
        var team = await GetTeamWithMembersQuery().FirstOrDefaultAsync(t => t.Id == teamId);
        if (team is null)
            throw new NotFoundException($"{nameof(Team)} not found");

        EnsureCaptain(team, currentUser.Id);
        var previousLogo = team.LogoUrl;
        var logoUrl = await _fileService.SaveImageAsync(logo);
        team.LogoUrl = logoUrl;
        await _dbContext.SaveChangesAsync();
        await _fileService.DeleteImageAsync(previousLogo);
        return new TeamLogoResponseDTO(team.Id, team.LogoUrl);
    }

    public async Task<TeamLogoResponseDTO> RemoveTeamLogoAsync(string auth0UserId, Guid teamId)
    {
        if (_fileService is null)
            throw new InvalidOperationException("File service is not configured.");

        var currentUser = await GetCurrentUserAsync(auth0UserId);
        var team = await GetTeamWithMembersQuery().FirstOrDefaultAsync(t => t.Id == teamId);
        if (team is null)
            throw new NotFoundException($"{nameof(Team)} not found");

        EnsureCaptain(team, currentUser.Id);
        var previousLogo = team.LogoUrl;
        team.LogoUrl = null;
        await _dbContext.SaveChangesAsync();
        await _fileService.DeleteImageAsync(previousLogo);
        return new TeamLogoResponseDTO(team.Id, null);
    }

    private static bool IsValidPublicUsername(string? username)
    {
        return !string.IsNullOrWhiteSpace(username);
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

    private async Task EnsureCaptainLimitAsync(Guid captainUserId, Guid? excludedTeamId = null)
    {
        var captainedTeamCount = await _dbContext.Teams.CountAsync(team =>
            !team.IsDeleted &&
            team.CaptainUserId == captainUserId &&
            (!excludedTeamId.HasValue || team.Id != excludedTeamId.Value));
        if (captainedTeamCount >= MaxCaptainedTeams)
            throw new ValidationException($"A user can captain at most {MaxCaptainedTeams} teams.");
    }

    private static void EnsureCaptain(Team team, Guid userId)
    {
        if (team.CaptainUserId != userId)
            throw new UnauthorizedAccessException("Only the team captain can perform this action.");
    }

    private async Task<bool> IsTeamInLeaveBlockingTournamentAsync(Guid teamId)
    {
        return await _dbContext.Games.AnyAsync(game =>
            game.ParticipationMode == ParticipationMode.Team &&
            (game.Status == GameStatus.InProgress || game.Status == GameStatus.Completed || game.Status == GameStatus.Canceled) &&
            game.RegisteredTeams.Any(team => team.Id == teamId));
    }

    private async Task<bool> IsTeamInMemberRemovalBlockingTournamentAsync(Guid teamId)
    {
        return await _dbContext.Games.AnyAsync(game =>
            game.ParticipationMode == ParticipationMode.Team &&
            game.Status == GameStatus.InProgress &&
            game.RegisteredTeams.Any(team => team.Id == teamId));
    }

    private async Task<bool> IsTeamInDeleteBlockingTournamentAsync(Guid teamId)
    {
        return await _dbContext.Games.AnyAsync(game =>
            game.ParticipationMode == ParticipationMode.Team &&
            (game.Status == GameStatus.Scheduled || game.Status == GameStatus.InProgress) &&
            game.RegisteredTeams.Any(team => team.Id == teamId));
    }

    private async Task ExpirePendingInvitesAsync(Guid? teamId = null, Guid? userId = null)
    {
        var now = DateTime.UtcNow;
        var query = _dbContext.TeamInvites.Where(invite =>
            invite.Status == TeamInviteStatus.Pending &&
            invite.ExpiresAt <= now &&
            (!teamId.HasValue || invite.TeamId == teamId.Value) &&
            (!userId.HasValue || invite.UserId == userId.Value));

        var invites = await query.ToListAsync();
        foreach (var invite in invites)
            invite.Expire();

        if (invites.Count > 0)
        {
            await _dbContext.SaveChangesAsync();
            foreach (var invite in invites)
                await _eventPublisher.InviteChangedAsync(invite.TeamId, invite.Id, invite.UserId, invite.Status.ToString());
        }
    }

    private async Task CleanupTerminalInvitesAsync()
    {
        var cutoff = DateTime.UtcNow.AddDays(-_inviteRetentionDays);
        var invites = await _dbContext.TeamInvites
            .Where(invite =>
                invite.Status != TeamInviteStatus.Pending &&
                (invite.RespondedAt ?? invite.CancelledAt ?? invite.ExpiredAt ?? invite.CreatedAt) < cutoff)
            .ToListAsync();

        if (invites.Count == 0)
            return;

        _dbContext.TeamInvites.RemoveRange(invites);
        await _dbContext.SaveChangesAsync();
    }

    private Task<bool> CheckIfTeamNameExistsAsync(string normalizedName, Guid? excludedTeamId = null)
    {
        return _dbContext.Teams.AnyAsync(t =>
            !t.IsDeleted &&
            t.NormalizedName == normalizedName &&
            (!excludedTeamId.HasValue || t.Id != excludedTeamId.Value));
    }

    private IQueryable<Team> GetTeamDetailsQuery()
    {
        return GetTeamWithMembersQuery()
            .Include(t => t.TeamInvites);
    }

    private IQueryable<Team> GetTeamWithMembersQuery()
    {
        return GetActiveTeamsQuery()
            .Include(t => t.Captain)
            .Include(t => t.Members);
    }

    private IQueryable<Team> TeamManagementQuery()
    {
        return GetActiveTeamsQuery()
            .AsNoTracking()
            .Include(t => t.Captain)
            .Include(t => t.Members);
    }

    private IQueryable<Team> GetActiveTeamsQuery()
    {
        return _dbContext.Teams.Where(team => !team.IsDeleted);
    }

    private IQueryable<TeamInvite> GetPendingInviteSummariesQuery()
    {
        return _dbContext.TeamInvites
            .AsNoTracking()
            .Include(invite => invite.Team)
            .Include(invite => invite.User)
            .Where(invite => !invite.Team.IsDeleted && invite.Status == TeamInviteStatus.Pending && invite.ExpiresAt > DateTime.UtcNow);
    }

    private async Task SaveTeamChangesAsync(string teamName)
    {
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsTeamNameUniqueConstraintViolation(exception))
        {
            throw new ValidationException($"Teamname {teamName} already in use");
        }
    }

    private static bool IsTeamNameUniqueConstraintViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current.Message.Contains("IX_Teams_NormalizedName", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private async Task SaveInviteChangesAsync()
    {
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsPendingInviteUniqueConstraintViolation(exception))
        {
            throw new ValidationException("User already has a pending invite to this team");
        }
    }

    private static bool IsPendingInviteUniqueConstraintViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current.Message.Contains("IX_TeamInvites_TeamId_UserId_Pending", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}

