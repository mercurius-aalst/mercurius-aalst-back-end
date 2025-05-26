using MercuriusAPI.DTOs.LAN.TeamDTOs;
using MercuriusAPI.Services.LAN.PlayerServices;
using MercuriusAPI.Services.LAN.TeamServices;
using Microsoft.AspNetCore.Mvc;

namespace MercuriusAPI.Controllers.LAN
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeamsController(ITeamService _teamService, IPlayerService _playerService) : ControllerBase
    {
        
        [HttpGet]
        public IEnumerable<GetTeamDTO> GetTeams()
        {
            return _teamService.GetAllTeams();
        }
        [HttpGet("{id}")]
        public async Task<GetTeamDTO> GetTeamAsync(int id)
        {
            return new GetTeamDTO(await _teamService.GetTeamByIdAsync(id));
        }
        [HttpPost]
        public async Task<GetTeamDTO> CreateTeamAsync(CreateTeamDTO createTeamDTO)
        {
            var captain = await _playerService.GetPlayerByIdAsync(createTeamDTO.CaptainId);
            return await _teamService.CreateTeamAsync(createTeamDTO, captain);
        }
        [HttpPut("{id}/players/{playerId}")]
        public async Task<GetTeamDTO> AddPlayerAsync(int id, int playerId)
        {
            var player = await _playerService.GetPlayerByIdAsync(playerId);
            return await _teamService.AddPlayerAsync(id, player);
        }

        [HttpDelete("{id}/players/{playerId}")]
        public Task<GetTeamDTO> RemoveMemberAsync(int id, int playerId)
        {
            return _teamService.RemovePlayerAsync(id, playerId);
        }

        [HttpDelete("{id}")]
        public Task DeleteTeamAsync(int id)
        {
            return _teamService.DeleteTeamAsync(id);
        }
    }
}
