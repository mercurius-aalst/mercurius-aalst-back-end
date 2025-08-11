using Asp.Versioning;
using MercuriusAPI.DTOs.LAN.GameDTOs;
using MercuriusAPI.DTOs.LAN.PlacementDTOs;
using MercuriusAPI.Services.LAN.GameServices;
using MercuriusAPI.Services.LAN.ParticipantServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MercuriusAPI.Controllers.LAN
{
    /// <summary>
    /// API endpoints for managing games, including creation, updates, participant registration, and game state transitions.
    /// </summary>
    [Authorize(Roles = "admin")]
    [Route("lan/[controller]")]
    [ApiVersion("1.0")]
    [ApiController]
    public class GamesController(IGameService _gameService, IParticipantService _participantService) : ControllerBase
    {
        /// <summary>
        /// Gets all games.
        /// </summary>
        /// <returns>A list of all games.</returns>
        [HttpGet]
        [AllowAnonymous]
        public IEnumerable<GetGameDTO> GetGames()
        {
            return _gameService.GetAllGames();
        }

        /// <summary>
        /// Gets a specific game by its ID.
        /// </summary>
        /// <param name="id">The game ID.</param>
        /// <returns>The game details.</returns>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<GetGameDTO> GetGameAsync(int id)
        {
            return new GetGameDTO(await _gameService.GetGameByIdAsync(id));
        }

        /// <summary>
        /// Creates a new game.
        /// </summary>
        /// <param name="createGameDTO">The game creation data.</param>
        /// <returns>The created game.</returns>
        [HttpPost]
        public Task<GetGameDTO> CreateGameAsync(CreateGameDTO createGameDTO)
        {
            return _gameService.CreateGameAsync(createGameDTO);
        }

        /// <summary>
        /// Updates an existing game.
        /// </summary>
        /// <param name="id">The game ID.</param>
        /// <param name="updateGameDTO">The updated game data.</param>
        /// <returns>The updated game.</returns>
        [HttpPut("{id}")]
        public Task<GetGameDTO> UpdateGameAsync(int id, UpdateGameDTO updateGameDTO)
        {
            return _gameService.UpdateGameAsync(id, updateGameDTO);
        }

        /// <summary>
        /// Deletes a game.
        /// </summary>
        /// <param name="id">The game ID.</param>
        [HttpDelete("{id}")]
        public Task DeleteGameAsync(int id)
        {
            return _gameService.DeleteGameAsync(id);
        }

        /// <summary>
        /// Registers a participant for a game.
        /// </summary>
        /// <param name="id">The game ID.</param>
        /// <param name="participantId">The participant ID.</param>
        /// <returns>The updated game with the new participant.</returns>
        [HttpPost("{id}/participants/{participantId}")]
        public async Task<GetGameDTO> RegisterForGameAsync(int id, int participantId)
        {
            var participant = await _participantService.GetParticipantByIdAsync(participantId);
            return await _gameService.AddParticipantAsync(id, participant);
        }

        /// <summary>
        /// Unregisters a participant from a game.
        /// </summary>
        /// <param name="id">The game ID.</param>
        /// <param name="participantId">The participant ID.</param>
        /// <returns>The updated game without the participant.</returns>
        [HttpDelete("{id}/participants/{participantId}")]
        public async Task<GetGameDTO> UnregisterFromGameAsync(int id, int participantId)
        {
            var participant = await _participantService.GetParticipantByIdAsync(participantId);
            return await _gameService.RemoveParticipantAsync(id, participant);
        }

        /// <summary>
        /// Starts a game.
        /// </summary>
        /// <param name="id">The game ID.</param>
        [HttpPost("{id}/start")]
        public Task StartGameAsync(int id)
        {
            return _gameService.StartGameAsync(id);
        }

        /// <summary>
        /// Resets a game to its initial state.
        /// </summary>
        /// <param name="id">The game ID.</param>
        [HttpPost("{id}/reset")]
        public Task ResetGameAsync(int id)
        {
            return _gameService.ResetGameAsync(id);
        }

        /// <summary>
        /// Completes a game and returns the final placements.
        /// </summary>
        /// <param name="id">The game ID.</param>
        /// <returns>Final placements of the game.</returns>
        [HttpPost("{id}/complete")]
        public Task<IEnumerable<GetPlacementDTO>> CompleteGameAsync(int id)
        {
            return _gameService.CompleteGameAsync(id);
        }

        /// <summary>
        /// Cancels a game.
        /// </summary>
        /// <param name="id">The game ID.</param>
        [HttpPost("{id}/cancel")]
        public Task CancelGameAsync(int id)
        {
            return _gameService.CancelGameAsync(id);
        }
    }
}
