using Asp.Versioning;
using MercuriusAPI.DTOs.LAN.PlayerDTOs;
using MercuriusAPI.Models.LAN;
using MercuriusAPI.Services.LAN.PlayerServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web;

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
        [AuthorizeForScopes(Scopes = ["Players.Read"])]
        [Authorize(Policy = "RequireLANAdmin")]
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
        [AuthorizeForScopes(Scopes = ["Players.Read"])]
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
        [AuthorizeForScopes(Scopes = ["Players.Manage"])]
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
        [AuthorizeForScopes(Scopes = ["Players.Manage"])]
        public async Task<GetPlayerDTO> UpdatePlayerAsync(int id, UpdatePlayerDTO updatePlayerDTO)
        {
            var userEntraObjectId = User.GetObjectId();
            var player = await _playerService.GetPlayerByIdAsync(id);

            if(player.Id != id)
                throw new UnauthorizedAccessException();
            return await _playerService.UpdatePlayerAsync(id, updatePlayerDTO);
        }

        /// <summary>
        /// Deletes a player.
        /// </summary>
        /// <param name="id">The player ID.</param>
        [HttpDelete("{id}")]
        [AuthorizeForScopes(Scopes = ["Players.Manage"])]
        public async Task DeletePlayerAsync(int id)
        {
            var userEntraObjectId = User.GetObjectId();
            var playerId = await _playerService.GetPlayerIdByEntraObjectIdAsync(userEntraObjectId);

            if(playerId != id)
                throw new UnauthorizedAccessException();
            await _playerService.DeletePlayerAsync(id);
        }
    }
}
