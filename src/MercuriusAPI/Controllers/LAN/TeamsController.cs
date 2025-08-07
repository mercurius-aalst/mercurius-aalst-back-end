using Asp.Versioning;
using MercuriusAPI.DTOs.LAN.TeamDTOs;
using MercuriusAPI.Services.LAN.PlayerServices;
using MercuriusAPI.Services.LAN.TeamServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace MercuriusAPI.Controllers.LAN
{
    /// <summary>
    /// API endpoints for managing teams, including creation, player management, and deletion.
    /// </summary>
    [Authorize(Roles = "Admin")]
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
        [AllowAnonymous]
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
        [AllowAnonymous]
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

        /// <summary>
        /// Invites a player to join a team. Only the team captain can send invites. If the player accepts, they will be added to the team.
        /// </summary>
        /// <param name="id">The ID of the team sending the invitation.</param>
        /// <param name="playerId">The ID of the player to invite.</param>
        /// <returns>The created team invitation details.</returns>
        [HttpPost("{id}/players/invite/{playerId}")]
        public async Task<TeamInviteDTO> InvitePlayerAsync(int id, int playerId)
        {
            return await _teamService.InvitePlayerAsync(id, playerId);
        }

        /// <summary>
        /// Allows a player to respond to a team invitation. The player can accept or decline the invitation.
        /// If accepted, the player is added to the team. If declined, the invitation is marked as declined.
        /// </summary>
        /// <param name="id">The ID of the team that sent the invitation.</param>
        /// <param name="playerId">The ID of the player responding to the invitation.</param>
        /// <param name="dto">The response indicating acceptance or decline.</param>
        /// <returns>The updated team invitation details.</returns>
        [HttpPut("{id}/players/invite/{playerId}")]
        public async Task<TeamInviteDTO> RespondToInviteAsync(int id, int playerId, [FromBody] RespondTeamInviteDTO dto)
        {
            return await _teamService.RespondToInviteAsync(id, playerId, dto.Accept);
        }

        /// <summary>
        /// Retrieves all pending team invitations for a specific player.
        /// </summary>
        /// <param name="playerId">The ID of the player whose invitations are being retrieved.</param>
        /// <returns>A list of pending team invitations for the player.</returns>
        [HttpGet("players/{playerId}/invites")]
        public async Task<IEnumerable<TeamInviteDTO>> GetPlayerInvitesAsync(int playerId)
        {
            return await _teamService.GetPlayerInvitesAsync(playerId);
        }
    }
}
