using MercuriusAPI.DTOs.LAN.PlayerDTOs;
using MercuriusAPI.Models.LAN;
using MercuriusAPI.Services.LAN.PlayerServices;
using Microsoft.AspNetCore.Mvc;

namespace MercuriusAPI.Controllers.LAN
{
    [Route("[controller]")]
    [ApiController]
    public class PlayersController : ControllerBase
    {
        private readonly IPlayerService _playerService;

        public PlayersController(IPlayerService playerService)
        {
            _playerService = playerService;
        }
        [HttpGet]
        public IEnumerable<GetPlayerDTO> GetPlayers()
        {
            return _playerService.GetAllPlayers();
        }
        [HttpGet("{id}")]
        public async Task<GetPlayerDTO> GetPlayerAsync(int id)
        {
            return new GetPlayerDTO(await _playerService.GetPlayerByIdAsync(id));
        }
        [HttpPost]
        public Task<GetPlayerDTO> CreatePlayerAsync(CreatePlayerDTO createPlayerDTO)
        {
            return _playerService.CreatePlayerAsync(createPlayerDTO);
        }
        [HttpPut("{id}")]
        public Task<GetPlayerDTO> UpdatePlayerAsync(int id, UpdatePlayerDTO updatePlayerDTO)
        {
            return _playerService.UpdatePlayerAsync(id, updatePlayerDTO);
        }
        [HttpDelete("{id}")]
        public Task DeletePlayerAsync(int id)
        {
            return _playerService.DeletePlayerAsync(id);
        }
    }
}
