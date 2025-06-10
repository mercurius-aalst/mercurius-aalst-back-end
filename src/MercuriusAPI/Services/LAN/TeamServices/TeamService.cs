using MercuriusAPI.Data;
using MercuriusAPI.DTOs.LAN.TeamDTOs;
using MercuriusAPI.Exceptions;
using MercuriusAPI.Models.LAN;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MercuriusAPI.Services.LAN.TeamServices
{
    public class TeamService : ITeamService
    {
        private readonly MercuriusDBContext _dbContext;
        private readonly int _inviteResendCooldownDays;
        public TeamService(MercuriusDBContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _inviteResendCooldownDays = configuration.GetSection("TeamInvite:ResendCooldownDays").Get<int>();
        }

        public async Task<GetTeamDTO> CreateTeamAsync(CreateTeamDTO teamDTO, Player captain)
        {
            if(await CheckIfTeamNameExistsAsync(teamDTO.Name))
                throw new ValidationException($"Teamname {teamDTO.Name} already in use");
            var team = new Team(teamDTO.Name, captain);
            _dbContext.Teams.Add(team);
            await _dbContext.SaveChangesAsync();
            return new GetTeamDTO(team);
        }
        public async Task DeleteTeamAsync(int teamId)
        {
            var team = await _dbContext.Teams.FindAsync(teamId);
            if(team is null)
                throw new NotFoundException($"{nameof(Team)} not found");
            _dbContext.Teams.Remove(team);
            await _dbContext.SaveChangesAsync();
        }
        public IEnumerable<GetTeamDTO> GetAllTeams()
        {
            return _dbContext.Teams.Include(t => t.Players).Select(t => new GetTeamDTO(t));
        }
        public async Task<Team> GetTeamByIdAsync(int teamId)
        {
            var team = await _dbContext.Teams.FindAsync(teamId);
            if(team is null)
                throw new NotFoundException($"{nameof(Team)} not found");
            await _dbContext.Entry(team).Collection(p => p.Players).LoadAsync();
            return team;
        }

        public async Task<GetTeamDTO> RemovePlayerAsync(int id, int playerId)
        {
            var team = await _dbContext.Teams.Include(t => t.Players).FirstOrDefaultAsync(t => t.Id == id);
            if(team is null)
                throw new NotFoundException($"{nameof(Team)} not found");
            team.RemovePlayer(playerId);
            await _dbContext.SaveChangesAsync();
            return new GetTeamDTO(team);
        }

        public async Task<GetTeamDTO> UpdateTeamAsync(int id, UpdateTeamDTO teamDTO)
        {
            var team = await GetTeamByIdAsync(id);
            if(teamDTO.Name != null && !team.Name.Equals(teamDTO.Name) && await CheckIfTeamNameExistsAsync(teamDTO.Name))
                throw new ValidationException($"Teamname {teamDTO.Name} already in use");
            team.Update(teamDTO.Name, teamDTO.CaptainId);
            _dbContext.Teams.Update(team);
            await _dbContext.SaveChangesAsync();
            return new GetTeamDTO(team);
        }

        public async Task<TeamInviteDTO> InvitePlayerAsync(int teamId, int playerId)
        {
            var team = await GetTeamByIdAsync(teamId);
            await _dbContext.Entry(team).Collection(t => t.TeamInvites).LoadAsync();
            if(team == null)
                throw new NotFoundException($"{nameof(Team)} not found");
            var invite = team.InvitePlayer(playerId, _inviteResendCooldownDays);
            await _dbContext.SaveChangesAsync();
            return new TeamInviteDTO(invite);
        }

        public async Task<TeamInviteDTO> RespondToInviteAsync(int teamId, int playerId, bool accept)
        {
            var invite = await _dbContext.TeamInvites.Include(ti => ti.Player).Include(ti => ti.Team).FirstOrDefaultAsync(i => i.TeamId == teamId && i.PlayerId == playerId && i.Status == TeamInviteStatus.Pending);
            if(invite == null)
                throw new NotFoundException("No pending invite not found");
            invite.Respond(accept);
            await _dbContext.SaveChangesAsync();
            return new TeamInviteDTO(invite);
        }

        public async Task<IEnumerable<TeamInviteDTO>> GetPlayerInvitesAsync(int playerId)
        {
            var invites = await _dbContext.TeamInvites.Where(i => i.PlayerId == playerId && i.Status == TeamInviteStatus.Pending).ToListAsync();
            return invites.Select(invite => new TeamInviteDTO
            {
                Id = invite.Id,
                TeamId = invite.TeamId,
                PlayerId = invite.PlayerId,
                Status = invite.Status.ToString(),
                CreatedAt = invite.CreatedAt
            });
        }

        private Task<bool> CheckIfTeamNameExistsAsync(string name)
        {
            return _dbContext.Teams.AnyAsync(t => t.Name.Equals(name));
        }
    }
}
