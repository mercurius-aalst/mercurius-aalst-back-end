using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.DTOs.TeamDTOs;
using Mercurius.LAN.API.DTOs.Public;
using Mercurius.LAN.API.Exceptions;
using Mercurius.LAN.API.Models;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.LAN.API.Services.TeamServices;

public class TeamService : ITeamService
{
    private readonly MercuriusDBContext _dbContext;
    private readonly int _inviteResendCooldownDays;
    public TeamService(MercuriusDBContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _inviteResendCooldownDays = configuration.GetSection("TeamInvite:ResendCooldownDays").Get<int>();
    }

    public async Task<GetTeamDTO> CreateTeamAsync(CreateTeamDTO teamDTO)
    {
        if (await CheckIfTeamNameExistsAsync(teamDTO.Name))
            throw new ValidationException($"Teamname {teamDTO.Name} already in use");
        var captain = await _dbContext.Users.FindAsync(teamDTO.CaptainUserId);
        if (captain is null)
            throw new NotFoundException($"{nameof(User)} not found");
        var team = new Team(teamDTO.Name, captain);
        _dbContext.Teams.Add(team);
        await _dbContext.SaveChangesAsync();
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
        return _dbContext.Teams
            .Include(t => t.Members)
            .Include(t => t.TeamInvites)
            .Select(t => new GetTeamDTO(t));
    }

    public IEnumerable<PublicTeamDTO> GetAllPublicTeams(PublicAudience audience)
    {
        return _dbContext.Teams
            .AsNoTracking()
            .SelectPublicTeams(audience)
            .ToList();
    }

    public async Task<Team> GetTeamByIdAsync(Guid teamId)
    {
        var team = await _dbContext.Teams
            .Include(t => t.Members)
            .Include(t => t.TeamInvites)
            .FirstOrDefaultAsync(t => t.Id == teamId);
        if (team is null)
            throw new NotFoundException($"{nameof(Team)} not found");
        return team;
    }

    public async Task<PublicTeamDTO> GetPublicTeamByIdAsync(Guid teamId, PublicAudience audience)
    {
        var team = await _dbContext.Teams
            .AsNoTracking()
            .Where(team => team.Id == teamId)
            .SelectPublicTeams(audience)
            .FirstOrDefaultAsync();
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
        var team = await GetTeamByIdAsync(id);
        if (teamDTO.Name != null && !team.Name.Equals(teamDTO.Name) && await CheckIfTeamNameExistsAsync(teamDTO.Name))
            throw new ValidationException($"Teamname {teamDTO.Name} already in use");

        if (teamDTO.Name != null)
            team.UpdateName(teamDTO.Name);

        if (teamDTO.CaptainUserId.HasValue)
            team.ChangeCaptain(teamDTO.CaptainUserId.Value);

        _dbContext.Teams.Update(team);
        await _dbContext.SaveChangesAsync();
        return new GetTeamDTO(team);
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

    private Task<bool> CheckIfTeamNameExistsAsync(string name)
    {
        return _dbContext.Teams.AnyAsync(t => t.Name.Equals(name));
    }

}

