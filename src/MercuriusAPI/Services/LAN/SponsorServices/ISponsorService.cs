using MercuriusAPI.DTOs.LAN.SponsorDTOs;

namespace MercuriusAPI.Services.LAN.SponsorServices;

public interface ISponsorService
{
    Task<GetSponsorDTO> CreateSponsorAsync(CreateSponsorDTO sponsorDTO);
    Task DeleteSponsorAsync(int id);
    Task<GetSponsorDTO> GetSponsorByIdAsync(int id);
    IEnumerable<GetSponsorDTO> GetSponsors();
    Task<GetSponsorDTO> UpdateSponsorAsync(int id, UpdateSponsorDTO sponsorDTO);
}