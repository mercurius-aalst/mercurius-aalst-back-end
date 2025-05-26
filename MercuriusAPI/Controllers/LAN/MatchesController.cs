using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MercuriusAPI.Controllers.LAN
{
    [Route("api/[controller]")]
    [ApiController]
    public class MatchesController : ControllerBase
    {
        [HttpGet]
        public Task GetMatchesAsync()
        {
            throw new NotImplementedException();
        }
        [HttpGet("{id}")]
        public Task GetMatchAsync(int id)
        {
            throw new NotImplementedException();
        }
        [HttpPost]
        public Task CreateMatchAsync()
        {
            throw new NotImplementedException();
        }
        [HttpPut("{id}")]
        public Task UpdateMatchAsync(int id)
        {
            throw new NotImplementedException();
        }
        [HttpDelete("{id}")]
        public Task DeleteMatchAsync(int id)
        {
            throw new NotImplementedException();
        }
    }
}
