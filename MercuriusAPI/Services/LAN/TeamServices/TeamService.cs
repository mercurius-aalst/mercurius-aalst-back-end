using MercuriusAPI.Data;
using MercuriusAPI.DTOs.LAN.TeamDTOs;
using MercuriusAPI.Models.LAN;
using Microsoft.EntityFrameworkCore;

namespace MercuriusAPI.Services.LAN.TeamServices
{
    public class TeamService : ITeamService
    {
        private readonly MercuriusDBContext _dbContext;
        public TeamService(MercuriusDBContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<GetTeamDTO> CreateTeamAsync(CreateTeamDTO teamDTO, Player captain)
        {
            if(await CheckIfTeamNameExistsAsync(teamDTO.Name))
                throw new Exception("Teamname already in use");            
            var team = new Team(teamDTO.Name, captain);
            _dbContext.Teams.Add(team);
            await _dbContext.SaveChangesAsync();
            return new GetTeamDTO(team);
        }
        public async Task DeleteTeamAsync(int teamId)
        {
            var team = await _dbContext.Teams.FindAsync(teamId);
            if(team is null)
                throw new Exception("Team not found");
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
                throw new Exception("Team not found");
            await _dbContext.Entry(team).Collection(p => p.Players).LoadAsync();
            return team;
        }

        public async Task<GetTeamDTO> RemovePlayerAsync(int id, int playerId)
        {
            var team = await _dbContext.Teams.Include(t => t.Players).FirstOrDefaultAsync(t => t.Id == id);
            if(team is null)
                throw new Exception("Team not found");
            var player = team.Players.FirstOrDefault(m => m.Id == playerId);
            if(player is null)
                throw new Exception("Player not found in team");
            if(playerId == team.CaptainId)
                throw new Exception("Captain cannot be removed from team");
            team.Players.Remove(player);
            await _dbContext.SaveChangesAsync();
            return new GetTeamDTO(team);
        }

        public async Task<GetTeamDTO> AddPlayerAsync(int id, Player player)
        {
            var team = await _dbContext.Teams.Include(t => t.Players).FirstOrDefaultAsync(t => t.Id == id);
            if(team is null)
                throw new Exception("Team not found");
            team.Players.Add(player);
            await _dbContext.SaveChangesAsync();
            return new GetTeamDTO(team);
        }

        public async Task<GetTeamDTO> UpdateTeamAsync(int id, UpdateTeamDTO teamDTO)
        {
            var team = await GetTeamByIdAsync(id);
            if(teamDTO.Name != null && !team.Name.Equals(teamDTO.Name) && await CheckIfTeamNameExistsAsync(teamDTO.Name))
                throw new Exception("Teamname already in use");
            team.Update(teamDTO.Name, teamDTO.CaptainId);
            _dbContext.Teams.Update(team);
            await _dbContext.SaveChangesAsync();
            return new GetTeamDTO(team);
        }

        private Task<bool> CheckIfTeamNameExistsAsync(string name)
        {
            return _dbContext.Teams.AnyAsync(t => t.Name.Equals(name));
        }
    }
}
