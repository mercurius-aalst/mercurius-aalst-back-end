using MercuriusAPI.DTOs.LAN.MatchDTOs;
using MercuriusAPI.Services.LAN.MatchServices;
using Microsoft.AspNetCore.Mvc;

namespace MercuriusAPI.Controllers.LAN
{
    [Route("api/[controller]")]
    [ApiController]
    public class MatchesController : ControllerBase
    {
        private readonly IMatchService _matchService;

        public MatchesController(IMatchService matchService)
        {
            _matchService = matchService;
        }
        [HttpPut("{id}")]
        public  Task<GetMatchDTO> UpdateMatchAsync(int id, UpdateMatchDTO updateMatchDTO)
        {
            return _matchService.UpdateMatchAsync(id, updateMatchDTO);          
        }

        [HttpGet("{id}")]
        public async Task<GetMatchDTO> GetMatchAsync(int id)
        {
            return new GetMatchDTO(await _matchService.GetMatchByIdAsync(id));
        }
    }
}
