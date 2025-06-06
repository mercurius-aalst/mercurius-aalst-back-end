using Asp.Versioning;
using MercuriusAPI.DTOs.LAN.TeamDTOs;
using MercuriusAPI.Services.LAN.PlayerServices;
using MercuriusAPI.Services.LAN.TeamServices;
using Microsoft.AspNetCore.Mvc;

namespace MercuriusAPI.Controllers.LAN
{
    /// <summary>
    /// API endpoints for managing teams, including creation, player management, and deletion.
    /// </summary>
    [Route("lan/[controller]")]
    [ApiVersion("1.0")]
    [ApiController]
    public class TeamsController(ITeamService _teamService, IPlayerService _playerService) : ControllerBase
    {
        /// <summary>
        /// Gets all teams.
        /// </summary>
        /// <returns>A list of all teams.</returns>
        [HttpGet]
        public IEnumerable<GetTeamDTO> GetTeams()
        {
            return _teamService.GetAllTeams();
        }

        /// <summary>
        /// Gets a specific team by ID.
        /// </summary>
        /// <param name="id">The team ID.</param>
        /// <returns>The team details.</returns>
        [HttpGet("{id}")]
        public async Task<GetTeamDTO> GetTeamAsync(int id)
        {
            return new GetTeamDTO(await _teamService.GetTeamByIdAsync(id));
        }

        /// <summary>
        /// Creates a new team.
        /// </summary>
        /// <param name="createTeamDTO">The team creation data.</param>
        /// <returns>The created team.</returns>
        [HttpPost]
        public async Task<GetTeamDTO> CreateTeamAsync(CreateTeamDTO createTeamDTO)
        {
            var captain = await _playerService.GetPlayerByIdAsync(createTeamDTO.CaptainId);
            return await _teamService.CreateTeamAsync(createTeamDTO, captain);
        }

        /// <summary>
        /// Adds a player to a team.
        /// </summary>
        /// <param name="id">The team ID.</param>
        /// <param name="playerId">The player ID.</param>
        /// <returns>The updated team with the new player.</returns>
        [HttpPut("{id}/players/{playerId}")]
        public async Task<GetTeamDTO> AddPlayerAsync(int id, int playerId)
        {
            var player = await _playerService.GetPlayerByIdAsync(playerId);
            return await _teamService.AddPlayerAsync(id, player);
        }

        /// <summary>
        /// Removes a player from a team.
        /// </summary>
        /// <param name="id">The team ID.</param>
        /// <param name="playerId">The player ID.</param>
        /// <returns>The updated team without the player.</returns>
        [HttpDelete("{id}/players/{playerId}")]
        public Task<GetTeamDTO> RemovePlayerAsync(int id, int playerId)
        {
            return _teamService.RemovePlayerAsync(id, playerId);
        }

        [HttpPut("{id}")]
        public Task<GetTeamDTO> UpdateTeamAsync(int id, UpdateTeamDTO updateTeamDTO)
        {
            return _teamService.UpdateTeamAsync(id, updateTeamDTO);
        }

        /// <summary>
        /// Deletes a team.
        /// </summary>
        /// <param name="id">The team ID.</param>
        [HttpDelete("{id}")]
        public Task DeleteTeamAsync(int id)
        {
            return _teamService.DeleteTeamAsync(id);
        }
    }
}
