using Mercurius.LAN.API.DTOs.SponsorDTOs;

namespace Mercurius.LAN.API.Services.SponsorServices;

public interface ISponsorService
{
    Task<GetSponsorDTO> CreateSponsorAsync(CreateSponsorDTO sponsorDTO);
    Task DeleteSponsorAsync(int id);
    Task<GetSponsorDTO> GetSponsorByIdAsync(int id);
    IEnumerable<GetSponsorDTO> GetSponsors();
    Task<GetSponsorDTO> UpdateSponsorAsync(int id, UpdateSponsorDTO sponsorDTO);
}
