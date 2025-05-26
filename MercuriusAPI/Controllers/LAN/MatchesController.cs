using MercuriusAPI.DTOs.LAN.MatchDTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MercuriusAPI.Controllers.LAN
{
    [Route("api/[controller]")]
    [ApiController]
    public class MatchesController : ControllerBase
    {
        [HttpPut("{id}")]
        public Task UpdateMatchAsync(int id, UpdateMatchDTO updateMatchDTO)
        {
            throw new NotImplementedException();
        }      
    }
}
