using MercuriusAPI.DTOs.LAN.SponsorDTOs;
using MercuriusAPI.Services.LAN.SponsorServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MercuriusAPI.Controllers.LAN;

[Route("lan/[controller]")]
[Authorize]
[ApiController]
public class SponsorsController
{
    private readonly ISponsorService _sponsorService;

    public SponsorsController(ISponsorService sponsorService)
    {
        _sponsorService = sponsorService;
    }


    [HttpGet]
    [AllowAnonymous]
    public IEnumerable<GetSponsorDTO> GetAll()
    {
        return _sponsorService.GetSponsors();
    }


    [HttpGet("{id}")]
    [AllowAnonymous]
    public Task<GetSponsorDTO> GetByIdAsync(int id)
    {
        return _sponsorService.GetSponsorByIdAsync(id);
    }

    [HttpPost]
    public Task<GetSponsorDTO> CreateAsync([FromForm] CreateSponsorDTO sponsorDTO)
    {
        return _sponsorService.CreateSponsorAsync(sponsorDTO);
    }

    [HttpPatch("{id}")]
    public Task<GetSponsorDTO> UpdateAsync(int id, [FromForm] UpdateSponsorDTO value)
    {
        return _sponsorService.UpdateSponsorAsync(id, value);
    }

    [HttpDelete("{id}")]
    public Task DeleteAsync(int id)
    {
        return _sponsorService.DeleteSponsorAsync(id);
    }
}
