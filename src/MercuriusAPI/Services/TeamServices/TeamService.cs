using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.DTOs.TeamDTOs;
using Mercurius.LAN.API.Exceptions;
using Mercurius.LAN.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.LAN.API.Services.TeamServices;

public class TeamService : ITeamService
{
    private const int MaxTeamSearchResults = 25;
    private readonly MercuriusDBContext _dbContext;
    private readonly int _inviteResendCooldownDays;
    public TeamService(MercuriusDBContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _inviteResendCooldownDays = configuration.GetSection("TeamInvite:ResendCooldownDays").Get<int>();
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
    public async Task DeleteTeamAsync(Guid teamId)
    {
        var team = await _dbContext.Teams.FindAsync(teamId);
        if (team is null)
            throw new NotFoundException($"{nameof(Team)} not found");
        _dbContext.Teams.Remove(team);
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
        var invite = team.InviteUser(userId, _inviteResendCooldownDays);
        await _dbContext.SaveChangesAsync();
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
            CreatedAt = invite.CreatedAt
        });
    }

    private static bool IsValidPublicUsername(string? username)
    {
        return !string.IsNullOrWhiteSpace(username);
    }

    private Task<bool> CheckIfTeamNameExistsAsync(string normalizedName, Guid? excludedTeamId = null)
    {
        return _dbContext.Teams.AnyAsync(t =>
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
        return _dbContext.Teams
            .Include(t => t.Members);
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
}

