using Asp.Versioning;
using MercuriusAPI.DTOs.LAN.MatchDTOs;
using MercuriusAPI.Services.LAN.MatchServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace MercuriusAPI.Controllers.LAN;

/// <summary>
/// API endpoints for managing matches, including retrieval and updates.
/// Handles operations related to matches in the LAN system.
/// </summary>
[Authorize(Roles = "admin")]
[Route("lan/[controller]")]
[ApiVersion("1.0")]
[ApiController]
public class MatchesController : ControllerBase
{
    private readonly IMatchService _matchService;


    public MatchesController(IMatchService matchService)
    {
        _matchService = matchService;
    }

    /// <summary>
    /// Updates the scores and results of a match.
    /// Winners and losers next match are possible also edited (if match is finished) => retrieve match by ID to get those updated versions.
    /// </summary>
    /// <param name="id">The match ID.</param>
    /// <param name="updateMatchDTO">The updated match data.</param>
    /// <returns>The updated match DTO.</returns>
    [HttpPut("{id}")]
    public Task<GetMatchDTO> UpdateMatchAsync(int id, UpdateMatchDTO updateMatchDTO)
    {
        return _matchService.UpdateMatchAsync(id, updateMatchDTO);
    }

    /// <summary>
    /// Retrieves a match by its ID.
    /// </summary>
    /// <param name="id">The match ID.</param>
    /// <returns>The match DTO.</returns>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<GetMatchDTO> GetMatchAsync(int id)
    {
        return new GetMatchDTO(await _matchService.GetMatchByIdAsync(id));
    }
}
