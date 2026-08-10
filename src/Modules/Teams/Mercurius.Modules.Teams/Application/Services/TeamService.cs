using Mercurius.Modules.Teams.DTOs;
using Mercurius.Modules.Shared.Exceptions;
using Mercurius.Modules.Teams.Contracts;
using Mercurius.Modules.Teams.Domain;
using Mercurius.Modules.Identity.Contracts;
using Mercurius.Modules.Identity.Domain;
using Mercurius.Modules.Media.Contracts;
using Mercurius.Modules.Shared;
using Mercurius.Modules.Teams.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

namespace Mercurius.Modules.Teams.Services;

internal sealed class TeamService : ITeamQueries, ITeamManagementCommands, ITeamInviteWorkflows, ITeamLogoCommands
{
    private const int MaxCaptainedTeams = 2;
    private const int MaxTeamSearchResults = 25;
    private readonly ITeamsDbContext _dbContext;
    private readonly IMediaModule _mediaModule;
    private readonly IIdentityModule _identityModule;
    private readonly ITeamCompetitionReadService _competitionReadService;
    private readonly int _inviteResendCooldownDays;
    private readonly int _inviteExpirationDays;
    private readonly int _declinedInviteResendLimit;
    private DbSet<TeamInvite> TeamInvites => _dbContext.Set<TeamInvite>();

    public TeamService(
        ITeamsDbContext dbContext,
        IConfiguration configuration,
        IIdentityModule identityModule,
        IMediaModule mediaModule,
        ITeamCompetitionReadService competitionReadService)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _mediaModule = mediaModule ?? throw new ArgumentNullException(nameof(mediaModule));
        _identityModule = identityModule ?? throw new ArgumentNullException(nameof(identityModule));
        _competitionReadService = competitionReadService ?? throw new ArgumentNullException(nameof(competitionReadService));
        _inviteResendCooldownDays = configuration.GetSection("TeamInvite:ResendCooldownDays").Get<int>();
        _inviteExpirationDays = configuration.GetSection("TeamInvite:ExpirationDays").Get<int?>() ?? 14;
        _declinedInviteResendLimit = configuration.GetSection("TeamInvite:DeclinedResendLimit").Get<int?>() ?? 3;
    }

    public async Task<GetTeamDTO> CreateTeamAsync(CreateTeamDTO teamDTO, CancellationToken cancellationToken = default)
    {
        var normalizedTeamName = Team.NormalizeName(teamDTO.Name);
        if (await CheckIfTeamNameExistsAsync(normalizedTeamName, cancellationToken: cancellationToken))
            throw new ValidationException($"Teamname {teamDTO.Name} already in use");
        var captain = await GetUserProfileAsync(teamDTO.CaptainUserId, cancellationToken);
        if (captain is null || captain.IsDeleted)
            throw new NotFoundException($"{nameof(User)} not found");
        var captainReference = GetUserReference(captain);
        var team = new Team(teamDTO.Name, captain.Id.Value)
        {
            Captain = captainReference
        };
        team.Members.Add(captainReference);
        _dbContext.Teams.Add(team);
        await SaveTeamChangesAsync(teamDTO.Name, cancellationToken);
        return new GetTeamDTO(team);
    }

    public async Task<TeamManagementSummaryDTO> CreateCurrentUserTeamAsync(string auth0UserId, CreateTeamDTO teamDTO, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(auth0UserId, cancellationToken);
        await EnsureCaptainLimitAsync(currentUser.Id.Value, cancellationToken: cancellationToken);

        var normalizedTeamName = Team.NormalizeName(teamDTO.Name);
        if (await CheckIfTeamNameExistsAsync(normalizedTeamName, cancellationToken: cancellationToken))
            throw new ValidationException($"Teamname {teamDTO.Name} already in use");

        var captainReference = GetUserReference(currentUser);
        var team = new Team(teamDTO.Name, currentUser.Id.Value)
        {
            Id = Guid.NewGuid(),
            Captain = captainReference
        };
        team.Members.Add(captainReference);
        _dbContext.Teams.Add(team);
        await SaveTeamChangesAsync(teamDTO.Name, cancellationToken);

        return new TeamManagementSummaryDTO(team);
    }

    public async Task DeleteTeamAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        var team = await _dbContext.Teams
            .AsSplitQuery()
            .Include(t => t.Members)
            .Include(t => t.TeamInvites)
            .FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);
        if (team is null)
            throw new NotFoundException($"{nameof(Team)} not found");
        team.Delete(DateTime.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteTeamAsync(string auth0UserId, Guid teamId, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(auth0UserId, cancellationToken);
        var team = await GetActiveTeamsQuery()
            .AsSplitQuery()
            .Include(t => t.Members)
            .Include(t => t.TeamInvites)
            .FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);
        if (team is null)
            throw new NotFoundException($"{nameof(Team)} not found");

        EnsureCaptain(team, currentUser.Id.Value);
        if (await _competitionReadService.IsTeamInDeleteBlockingTournamentAsync(teamId, cancellationToken))
            throw new ValidationException("Cannot delete a team that is actively participating in a game or tournament.");

        team.Delete(DateTime.UtcNow);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<GetTeamDTO>> GetAllTeamsAsync(CancellationToken cancellationToken = default)
    {
        var teams = await GetTeamReadQuery()
            .ToListAsync(cancellationToken);

        return teams;
    }

    public async Task<GetTeamDTO> GetTeamByIdAsync(Guid teamId, CancellationToken cancellationToken = default)
    {
        var team = await ProjectTeamReadQuery(GetActiveTeamsQuery().Where(t => t.Id == teamId))
            .FirstOrDefaultAsync(cancellationToken);
        if (team is null)
            throw new NotFoundException($"{nameof(Team)} not found");
        return team;
    }

    public async Task<PublicTeamProfileDTO> GetPublicTeamProfileAsync(string teamName, CancellationToken cancellationToken = default)
    {
        var normalizedTeamName = Team.NormalizeName(teamName);
        var team = await _dbContext.Teams
            .AsNoTracking()
            .Where(t => !t.IsDeleted)
            .Where(t => t.NormalizedName == normalizedTeamName)
            .Select(t => new
            {
                t.Id,
                t.Name,
                CaptainUsername = t.Captain == null ? null : t.Captain.Username,
                t.LogoUrl,
                MemberUsernames = t.Members
                    .Select(member => member.Username)
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (team is null)
            throw new NotFoundException($"{nameof(Team)} not found");

        var tournaments = await _competitionReadService.GetPublicTeamTournamentsAsync(team.Id, cancellationToken);

        var members = team.MemberUsernames
            .Where(IsValidPublicUsername)
            .Select(username => username!)
            .OrderBy(username => username, StringComparer.OrdinalIgnoreCase)
            .ThenBy(username => username, StringComparer.Ordinal)
            .Select(username => new PublicTeamMemberDTO(username))
            .ToList();

        var captainUsername = team.CaptainUsername;

        return new PublicTeamProfileDTO
        {
            TeamName = team.Name,
            CaptainUsername = IsValidPublicUsername(captainUsername) ? captainUsername : null,
            LogoUrl = team.LogoUrl,
            Members = members,
            Tournaments = tournaments
                .Select(tournament => new PublicTeamTournamentDTO
                {
                    GameId = tournament.GameId.Value,
                    Name = tournament.Name
                })
                .ToList()
        };
    }

    public async Task<GetTeamDTO> GetTeamByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = Team.NormalizeName(name);
        var team = await ProjectTeamReadQuery(GetActiveTeamsQuery().Where(t => t.NormalizedName == normalizedName))
            .FirstOrDefaultAsync(cancellationToken);
        if (team is null)
            throw new NotFoundException($"{nameof(Team)} not found");
        return team;
    }

    public async Task<GetTeamDTO> RemoveMemberAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var team = await _dbContext.Teams.Include(t => t.Members).FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (team is null)
            throw new NotFoundException($"{nameof(Team)} not found");
        team.RemoveMember(userId);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new GetTeamDTO(team);
    }

    public async Task<TeamManagementSummaryDTO> RemoveMemberAsync(string auth0UserId, Guid teamId, Guid userId, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(auth0UserId, cancellationToken);
        var team = await GetTeamWithMembersQuery().FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);
        if (team is null)
            throw new NotFoundException($"{nameof(Team)} not found");

        EnsureCaptain(team, currentUser.Id.Value);
        if (await _competitionReadService.IsUserInProtectedTournamentRosterAsync(teamId, userId, cancellationToken))
            throw new ValidationException("Cannot remove a member from a team that is part of an in-progress tournament roster.");

        team.RemoveMember(userId);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new TeamManagementSummaryDTO(team);
    }

    public async Task<GetTeamDTO> UpdateTeamAsync(Guid id, UpdateTeamDTO teamDTO, CancellationToken cancellationToken = default)
    {
        var team = await GetTeamWithMembersQuery()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (team is null)
            throw new NotFoundException($"{nameof(Team)} not found");

        if (teamDTO.Name != null)
        {
            var normalizedTeamName = Team.NormalizeName(teamDTO.Name);
            var nameChanged = !string.Equals(team.NormalizedName, normalizedTeamName, StringComparison.Ordinal);
            if (nameChanged &&
                await CheckIfTeamNameExistsAsync(normalizedTeamName, id, cancellationToken))
            {
                throw new ValidationException($"Teamname {teamDTO.Name} already in use");
            }

            team.UpdateName(teamDTO.Name);
        }

        if (teamDTO.CaptainUserId.HasValue && teamDTO.CaptainUserId.Value != team.CaptainUserId)
        {
            team.ChangeCaptain(teamDTO.CaptainUserId.Value);
        }

        _dbContext.Teams.Update(team);
        await SaveTeamChangesAsync(team.Name, cancellationToken);
        return new GetTeamDTO(team);
    }

    public async Task<IEnumerable<GetTeamDTO>> SearchTeamsByNameAsync(string query, int? limit = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var normalizedQuery = query.Trim().ToLowerInvariant();
        var resultLimit = Math.Clamp(limit.GetValueOrDefault(MaxTeamSearchResults), 1, MaxTeamSearchResults);

        var teams = await ProjectTeamReadQuery(GetActiveTeamsQuery().Where(t => t.NormalizedName.StartsWith(normalizedQuery)))
            .OrderBy(t => t.Name)
            .Take(resultLimit)
            .ToListAsync(cancellationToken);

        return teams;
    }

    public async Task<TeamInviteDTO> InviteUserAsync(Guid teamId, Guid userId, CancellationToken cancellationToken = default)
    {
        var team = await GetTeamForInviteMutationAsync(teamId, cancellationToken);
        if (await GetUserProfileAsync(userId, cancellationToken) is null)
            throw new NotFoundException($"{nameof(User)} not found");
        await ExpirePendingInviteAsync(teamId, userId, cancellationToken);
        var invite = team.InviteUser(userId, _inviteResendCooldownDays, _inviteExpirationDays, _declinedInviteResendLimit);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new TeamInviteDTO(invite);
    }

    public async Task<TeamInviteDTO> InviteUserAsync(string auth0UserId, Guid teamId, Guid userId, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(auth0UserId, cancellationToken);
        var team = await GetTeamForInviteMutationAsync(teamId, cancellationToken);
        EnsureCaptain(team, currentUser.Id.Value);

        var invitedUser = await GetUserProfileAsync(userId, cancellationToken);
        if (invitedUser is null || invitedUser.IsDeleted)
            throw new NotFoundException($"{nameof(User)} not found");

        await ExpirePendingInviteAsync(teamId, userId, cancellationToken);
        var invite = team.InviteUser(userId, _inviteResendCooldownDays, _inviteExpirationDays, _declinedInviteResendLimit);
        await SaveInviteChangesAsync(cancellationToken);
        return new TeamInviteDTO(invite);
    }

    public async Task<TeamInviteDTO> CancelInviteAsync(string auth0UserId, Guid teamId, Guid inviteId, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(auth0UserId, cancellationToken);
        var invite = await TeamInvites
            .Include(i => i.Team)
            .FirstOrDefaultAsync(i => i.Id == inviteId && i.TeamId == teamId, cancellationToken);
        if (invite is null)
            throw new NotFoundException("Invite not found");

        EnsureCaptain(invite.Team, currentUser.Id.Value);
        invite.Cancel();
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new TeamInviteDTO(invite);
    }

    public async Task<TeamInviteDTO> RespondToInviteAsync(Guid teamId, Guid userId, bool accept, CancellationToken cancellationToken = default)
    {
        var invite = await TeamInvites
            .Include(ti => ti.User)
            .Include(ti => ti.Team)
            .FirstOrDefaultAsync(i => i.TeamId == teamId && i.UserId == userId && i.Status == TeamInviteStatus.Pending, cancellationToken);
        if (invite == null)
            throw new NotFoundException("No pending invite found");
        await _dbContext.Entry(invite.Team).Collection(t => t.Members).LoadAsync(cancellationToken);
        invite.Respond(accept);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new TeamInviteDTO(invite);
    }

    public async Task<TeamInviteDTO> RespondToInviteAsync(string auth0UserId, Guid inviteId, bool accept, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(auth0UserId, cancellationToken);
        var invite = await TeamInvites
            .Include(ti => ti.User)
            .Include(ti => ti.Team)
            .FirstOrDefaultAsync(i => i.Id == inviteId && i.UserId == currentUser.Id.Value, cancellationToken);
        if (invite == null)
            throw new NotFoundException("No pending invite found");

        await _dbContext.Entry(invite.Team).Collection(t => t.Members).LoadAsync(cancellationToken);
        invite.Respond(accept);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new TeamInviteDTO(invite);
    }

    public async Task<IEnumerable<TeamInviteDTO>> GetUserInvitesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var invites = await TeamInvites
            .AsNoTracking()
            .Where(i => i.UserId == userId && i.Status == TeamInviteStatus.Pending && i.ExpiresAt > now)
            .Select(invite => new TeamInviteDTO
            {
                Id = invite.Id,
                TeamId = invite.TeamId,
                UserId = invite.UserId,
                Status = nameof(TeamInviteStatus.Pending),
                CreatedAt = invite.CreatedAt,
                ExpiresAt = invite.ExpiresAt
            })
            .ToListAsync(cancellationToken);
        return invites;
    }

    public async Task<CurrentUserTeamSummaryDTO> GetCurrentUserTeamSummaryAsync(string auth0UserId, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(auth0UserId, cancellationToken);
        var now = DateTime.UtcNow;

        var captainedTeams = await ProjectTeamManagementSummaryQuery(
                GetActiveTeamsQuery().Where(team => team.CaptainUserId == currentUser.Id.Value))
            .OrderBy(team => team.Name)
            .ToListAsync(cancellationToken);

        var memberTeams = await ProjectTeamManagementSummaryQuery(
                GetActiveTeamsQuery().Where(team => team.Members.Any(member => member.Id == currentUser.Id.Value)))
            .OrderBy(team => team.Name)
            .ToListAsync(cancellationToken);

        return new CurrentUserTeamSummaryDTO
        {
            CaptainedTeams = captainedTeams,
            MemberTeams = memberTeams,
            ReceivedPendingInvites = await GetPendingInviteSummariesQuery(now)
                .Where(invite => invite.UserId == currentUser.Id.Value)
                .OrderBy(invite => invite.CreatedAt)
                .Select(invite => invite.Summary)
                .ToListAsync(cancellationToken),
            SentPendingInvites = await GetPendingInviteSummariesQuery(now)
                .Where(invite => invite.CaptainUserId == currentUser.Id.Value)
                .OrderBy(invite => invite.CreatedAt)
                .Select(invite => invite.Summary)
                .ToListAsync(cancellationToken)
        };
    }

    public async Task<IEnumerable<TeamInviteSummaryDTO>> GetCurrentUserInvitesAsync(string auth0UserId, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(auth0UserId, cancellationToken);
        var invites = await GetPendingInviteSummariesQuery(DateTime.UtcNow)
            .Where(invite => invite.UserId == currentUser.Id.Value)
            .OrderBy(invite => invite.CreatedAt)
            .Select(invite => invite.Summary)
            .ToListAsync(cancellationToken);
        return invites;
    }

    public async Task<IEnumerable<TeamInviteSummaryDTO>> GetCurrentUserSentInvitesAsync(string auth0UserId, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(auth0UserId, cancellationToken);
        var invites = await GetPendingInviteSummariesQuery(DateTime.UtcNow)
            .Where(invite => invite.CaptainUserId == currentUser.Id.Value)
            .OrderBy(invite => invite.CreatedAt)
            .Select(invite => invite.Summary)
            .ToListAsync(cancellationToken);
        return invites;
    }

    public async Task<TeamManagementSummaryDTO> LeaveTeamAsync(string auth0UserId, Guid teamId, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(auth0UserId, cancellationToken);
        var team = await GetTeamWithMembersQuery().FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);
        if (team is null)
            throw new NotFoundException($"{nameof(Team)} not found");
        if (team.CaptainUserId == currentUser.Id.Value)
            throw new ValidationException("The captain cannot leave a team without transferring captainship.");
        if (!team.Members.Any(member => member.Id == currentUser.Id.Value))
            throw new NotFoundException($"{nameof(User)} not found in {team.Name}");
        if (await _competitionReadService.IsUserInProtectedTournamentRosterAsync(teamId, currentUser.Id.Value, cancellationToken))
            throw new ValidationException("Cannot leave a team that is part of a protected tournament roster.");

        team.RemoveMember(currentUser.Id.Value);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new TeamManagementSummaryDTO(team);
    }

    public async Task<TeamManagementSummaryDTO> TransferCaptainAsync(string auth0UserId, Guid teamId, Guid newCaptainUserId, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(auth0UserId, cancellationToken);
        var team = await GetTeamWithMembersQuery()
            .Include(t => t.Captain)
            .FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);
        if (team is null)
            throw new NotFoundException($"{nameof(Team)} not found");

        EnsureCaptain(team, currentUser.Id.Value);
        await EnsureCaptainLimitAsync(newCaptainUserId, teamId, cancellationToken);
        team.ChangeCaptain(newCaptainUserId);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return new TeamManagementSummaryDTO(team);
    }

    public async Task<TeamLogoResponseDTO> UploadTeamLogoAsync(string auth0UserId, Guid teamId, IFormFile logo, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(auth0UserId, cancellationToken);
        var team = await GetTeamWithMembersQuery().FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);
        if (team is null)
            throw new NotFoundException($"{nameof(Team)} not found");

        EnsureCaptain(team, currentUser.Id.Value);
        var previousLogo = team.LogoUrl;
        await using var imageStream = logo.OpenReadStream();
        var asset = await _mediaModule.SaveImageAsync(
            new MediaUpload(imageStream, logo.FileName, logo.ContentType, logo.Length),
            cancellationToken);
        team.LogoUrl = asset.Url;
        cancellationToken.ThrowIfCancellationRequested();
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _mediaModule.DeleteImageAsync(previousLogo);
        return new TeamLogoResponseDTO(team.Id, team.LogoUrl);
    }

    public async Task<TeamLogoResponseDTO> RemoveTeamLogoAsync(string auth0UserId, Guid teamId, CancellationToken cancellationToken = default)
    {
        var currentUser = await GetCurrentUserAsync(auth0UserId, cancellationToken);
        var team = await GetTeamWithMembersQuery().FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);
        if (team is null)
            throw new NotFoundException($"{nameof(Team)} not found");

        EnsureCaptain(team, currentUser.Id.Value);
        var previousLogo = team.LogoUrl;
        team.LogoUrl = null;
        cancellationToken.ThrowIfCancellationRequested();
        await _dbContext.SaveChangesAsync(cancellationToken);
        await _mediaModule.DeleteImageAsync(previousLogo);
        return new TeamLogoResponseDTO(team.Id, null);
    }

    private static bool IsValidPublicUsername(string? username)
    {
        return !string.IsNullOrWhiteSpace(username);
    }

    private async Task<UserProfileSummary> GetCurrentUserAsync(string auth0UserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(auth0UserId))
            throw new UnauthorizedAccessException("Authenticated user id is missing.");

        var user = await _identityModule.GetUserProfileByAuth0IdAsync(auth0UserId, cancellationToken);

        if (user is null || user.IsDeleted)
            throw new NotFoundException("Current user profile was not found.");

        return user;
    }

    private async Task<UserProfileSummary?> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _identityModule.GetUserProfileAsync(new UserId(userId), cancellationToken);
    }

    private User GetUserReference(UserProfileSummary user)
    {
        var trackedUser = _dbContext.ChangeTracker
            .Entries<User>()
            .FirstOrDefault(entry => entry.Entity.Id == user.Id.Value)
            ?.Entity;
        if (trackedUser is not null)
            return trackedUser;

        var userReference = new User
        {
            Id = user.Id.Value,
            Username = user.Username,
            DiscordId = user.DiscordId,
            SteamId = user.SteamId,
            RiotId = user.RiotId
        };
        _dbContext.Users.Attach(userReference);
        return userReference;
    }

    private async Task EnsureCaptainLimitAsync(Guid captainUserId, Guid? excludedTeamId = null, CancellationToken cancellationToken = default)
    {
        var captainedTeamCount = await _dbContext.Teams.CountAsync(team =>
            !team.IsDeleted &&
            team.CaptainUserId == captainUserId &&
            (!excludedTeamId.HasValue || team.Id != excludedTeamId.Value),
            cancellationToken);
        if (captainedTeamCount >= MaxCaptainedTeams)
            throw new ValidationException($"A user can captain at most {MaxCaptainedTeams} teams.");
    }

    private static void EnsureCaptain(Team team, Guid userId)
    {
        if (team.CaptainUserId != userId)
            throw new UnauthorizedAccessException("Only the team captain can perform this action.");
    }

    private async Task ExpirePendingInviteAsync(Guid teamId, Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var invites = await TeamInvites
            .Where(invite =>
                invite.TeamId == teamId &&
                invite.UserId == userId &&
                invite.Status == TeamInviteStatus.Pending &&
                invite.ExpiresAt <= now)
            .ToListAsync(cancellationToken);
        foreach (var invite in invites)
            invite.Expire();

        if (invites.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private Task<bool> CheckIfTeamNameExistsAsync(string normalizedName, Guid? excludedTeamId = null, CancellationToken cancellationToken = default)
    {
        return _dbContext.Teams.AnyAsync(t =>
            !t.IsDeleted &&
            t.NormalizedName == normalizedName &&
            (!excludedTeamId.HasValue || t.Id != excludedTeamId.Value),
            cancellationToken);
    }

    private async Task<Team> GetTeamForInviteMutationAsync(Guid teamId, CancellationToken cancellationToken)
    {
        var team = await GetTeamDetailsQuery()
            .FirstOrDefaultAsync(t => t.Id == teamId, cancellationToken);
        if (team is null)
            throw new NotFoundException($"{nameof(Team)} not found");

        return team;
    }

    private IQueryable<Team> GetTeamDetailsQuery()
    {
        return GetTeamWithMembersQuery()
            .AsSplitQuery()
            .Include(t => t.TeamInvites);
    }

    private IQueryable<Team> GetTeamWithMembersQuery()
    {
        return GetActiveTeamsQuery()
            .Include(t => t.Captain)
            .Include(t => t.Members);
    }

    private IQueryable<GetTeamDTO> GetTeamReadQuery()
    {
        return ProjectTeamReadQuery(GetActiveTeamsQuery());
    }

    private static IQueryable<GetTeamDTO> ProjectTeamReadQuery(IQueryable<Team> query)
    {
        return query
            .AsNoTracking()
            .Select(team => new GetTeamDTO
            {
                Id = team.Id,
                Name = team.Name,
                CaptainUserId = team.CaptainUserId ?? Guid.Empty,
                LogoUrl = team.LogoUrl,
                Members = team.Members
                    .Select(member => new TeamPublicUserDTO
                    {
                        Id = member.Id,
                        Username = member.Username == null || member.Username == string.Empty ? "Incomplete profile" : member.Username,
                        DisplayName = member.Username == null || member.Username == string.Empty ? "Incomplete profile" : member.Username,
                        DiscordId = member.DiscordId,
                        SteamId = member.SteamId,
                        RiotId = member.RiotId
                    })
                    .ToList()
            });
    }

    private static IQueryable<TeamManagementSummaryDTO> ProjectTeamManagementSummaryQuery(IQueryable<Team> query)
    {
        return query
            .AsNoTracking()
            .Select(team => new TeamManagementSummaryDTO
            {
                Id = team.Id,
                Name = team.Name,
                CaptainUserId = team.CaptainUserId ?? Guid.Empty,
                CaptainUsername = team.Captain == null ? null : team.Captain.Username,
                LogoUrl = team.LogoUrl,
                Members = team.Members
                    .Select(member => new TeamPublicUserDTO
                    {
                        Id = member.Id,
                        Username = member.Username == null || member.Username == string.Empty ? "Incomplete profile" : member.Username,
                        DisplayName = member.Username == null || member.Username == string.Empty ? "Incomplete profile" : member.Username,
                        DiscordId = member.DiscordId,
                        SteamId = member.SteamId,
                        RiotId = member.RiotId
                    })
                    .ToList()
            });
    }

    private IQueryable<Team> GetActiveTeamsQuery()
    {
        return _dbContext.Teams.Where(team => !team.IsDeleted);
    }

    private IQueryable<TeamInviteSummaryQueryRow> GetPendingInviteSummariesQuery(DateTime now)
    {
        return TeamInvites
            .AsNoTracking()
            .Where(invite => !invite.Team.IsDeleted && invite.Status == TeamInviteStatus.Pending && invite.ExpiresAt > now)
            .Select(invite => new TeamInviteSummaryQueryRow
            {
                Summary = new TeamInviteSummaryDTO
                {
                    Id = invite.Id,
                    TeamId = invite.TeamId,
                    TeamName = invite.Team.Name,
                    TeamLogoUrl = invite.Team.LogoUrl,
                    UserId = invite.UserId,
                    Username = invite.User.Username,
                    Status = nameof(TeamInviteStatus.Pending),
                    CreatedAt = invite.CreatedAt,
                    ExpiresAt = invite.ExpiresAt
                },
                CaptainUserId = invite.Team.CaptainUserId,
                UserId = invite.UserId,
                CreatedAt = invite.CreatedAt
            });
    }

    private async Task SaveTeamChangesAsync(string teamName, CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
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

    private async Task SaveInviteChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
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

    private sealed class TeamInviteSummaryQueryRow
    {
        public TeamInviteSummaryDTO Summary { get; set; } = new();
        public Guid? CaptainUserId { get; set; }
        public Guid UserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

