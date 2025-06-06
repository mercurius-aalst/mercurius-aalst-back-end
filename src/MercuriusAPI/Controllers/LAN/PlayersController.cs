using Asp.Versioning;
using MercuriusAPI.DTOs.LAN.PlayerDTOs;
using MercuriusAPI.Models.LAN;
using MercuriusAPI.Services.LAN.PlayerServices;
using Microsoft.AspNetCore.Mvc;

namespace MercuriusAPI.Controllers.LAN
{
    /// <summary>
    /// API endpoints for managing players, including creation, retrieval, update, and deletion.
    /// </summary>
    [Route("lan/[controller]")]
    [ApiVersion("1.0")]
    [ApiController]
    public class PlayersController : ControllerBase
    {
        private readonly IPlayerService _playerService;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayersController"/> class.
        /// </summary>
        /// <param name="playerService">The player service to use for player operations.</param>
        public PlayersController(IPlayerService playerService)
        {
            _playerService = playerService;
        }

        /// <summary>
        /// Gets all players.
        /// </summary>
        /// <returns>A list of all players.</returns>
        [HttpGet]
        public IEnumerable<GetPlayerDTO> GetPlayers()
        {
            return _playerService.GetAllPlayers();
        }

        /// <summary>
        /// Gets a specific player by ID.
        /// </summary>
        /// <param name="id">The player ID.</param>
        /// <returns>The player details.</returns>
        [HttpGet("{id}")]
        public async Task<GetPlayerDTO> GetPlayerAsync(int id)
        {
            return new GetPlayerDTO(await _playerService.GetPlayerByIdAsync(id));
        }

        /// <summary>
        /// Creates a new player.
        /// </summary>
        /// <param name="createPlayerDTO">The player creation data.</param>
        /// <returns>The created player.</returns>
        [HttpPost]
        public Task<GetPlayerDTO> CreatePlayerAsync(CreatePlayerDTO createPlayerDTO)
        {
            return _playerService.CreatePlayerAsync(createPlayerDTO);
        }

        /// <summary>
        /// Updates an existing player.
        /// </summary>
        /// <param name="id">The player ID.</param>
        /// <param name="updatePlayerDTO">The updated player data.</param>
        /// <returns>The updated player.</returns>
        [HttpPut("{id}")]
        public Task<GetPlayerDTO> UpdatePlayerAsync(int id, UpdatePlayerDTO updatePlayerDTO)
        {
            return _playerService.UpdatePlayerAsync(id, updatePlayerDTO);
        }

        /// <summary>
        /// Deletes a player.
        /// </summary>
        /// <param name="id">The player ID.</param>
        [HttpDelete("{id}")]
        public Task DeletePlayerAsync(int id)
        {
            return _playerService.DeletePlayerAsync(id);
        }
    }
}
