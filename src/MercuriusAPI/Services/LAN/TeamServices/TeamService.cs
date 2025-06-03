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

        public async Task<GetTeamDTO> AddPlayerAsync(int id, Player player)
        {
            var team = await _dbContext.Teams.Include(t => t.Players).FirstOrDefaultAsync(t => t.Id == id);
            if(team is null)
                throw new NotFoundException($"{nameof(Team)} not found");
            team.Players.Add(player);
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
            var team = await _dbContext.Teams.Include(t => t.Players).FirstOrDefaultAsync(t => t.Id == teamId);
            if (team == null) throw new NotFoundException($"{nameof(Team)} not found");
            if (team.Players.Any(p => p.Id == playerId)) throw new ValidationException("Player is already in the team");

            // Check for pending invite
            var existingPendingInvite = await _dbContext.TeamInvites.FirstOrDefaultAsync(i => i.TeamId == teamId && i.PlayerId == playerId && i.Status == TeamInviteStatus.Pending);
            if (existingPendingInvite != null) throw new ValidationException("There is already a pending invite for this player");

            // Check for last declined invite
            var lastDeclinedInvite = await _dbContext.TeamInvites
                .Where(i => i.TeamId == teamId && i.PlayerId == playerId && i.Status == TeamInviteStatus.Declined)
                .OrderByDescending(i => i.RespondedAt)
                .FirstOrDefaultAsync();
            if (lastDeclinedInvite != null && lastDeclinedInvite.RespondedAt.HasValue)
            {
                var daysSinceDeclined = (DateTime.UtcNow - lastDeclinedInvite.RespondedAt.Value).TotalDays;
                if (daysSinceDeclined < _inviteResendCooldownDays)
                {
                    throw new ValidationException($"Player declined the last invite less than {_inviteResendCooldownDays} days ago. Please wait {_inviteResendCooldownDays - (int)daysSinceDeclined} more day(s).");
                }
            }

            var invite = new TeamInvite { TeamId = teamId, PlayerId = playerId };
            _dbContext.TeamInvites.Add(invite);
            await _dbContext.SaveChangesAsync();
            return new TeamInviteDTO
            {
                Id = invite.Id,
                TeamId = invite.TeamId,
                PlayerId = invite.PlayerId,
                Status = invite.Status.ToString(),
                CreatedAt = invite.CreatedAt
            };
        }

        public async Task<TeamInviteDTO> RespondToInviteAsync(int teamId, Player player, bool accept)
        {
            var invite = await _dbContext.TeamInvites.FirstOrDefaultAsync(i => i.TeamId == teamId && i.PlayerId == player.Id && i.Status == TeamInviteStatus.Pending);
            if (invite == null) throw new NotFoundException("Invite not found");
            invite.Status = accept ? TeamInviteStatus.Accepted : TeamInviteStatus.Declined;
            invite.RespondedAt = DateTime.UtcNow;
            if (accept)
            {
                var team = await _dbContext.Teams.Include(t => t.Players).FirstOrDefaultAsync(t => t.Id == teamId);
                if (team == null) throw new NotFoundException("Team not found");
                team.Players.Add(player);
            }
            await _dbContext.SaveChangesAsync();
            return new TeamInviteDTO
            {
                Id = invite.Id,
                TeamId = invite.TeamId,
                PlayerId = invite.PlayerId,
                Status = invite.Status.ToString(),
                CreatedAt = invite.CreatedAt,
                RespondedAt = invite.RespondedAt
            };
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
