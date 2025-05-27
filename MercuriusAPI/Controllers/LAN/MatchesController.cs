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
        public  Task<IEnumerable<GetMatchDTO>> UpdateMatchAsync(int id, UpdateMatchDTO updateMatchDTO)
        {
            return _matchService.UpdateMatchAsync(id, updateMatchDTO);          
        }
    }
}
