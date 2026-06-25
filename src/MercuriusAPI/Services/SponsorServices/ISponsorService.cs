using Mercurius.LAN.API.DTOs.SponsorDTOs;

namespace Mercurius.LAN.API.Services.SponsorServices;

public interface ISponsorService
{
    Task<GetSponsorDTO> CreateSponsorAsync(CreateSponsorDTO sponsorDTO);
    Task DeleteSponsorAsync(int id);
    Task<GetSponsorDTO> GetSponsorByIdAsync(int id);
    Task<IReadOnlyList<GetSponsorDTO>> GetSponsorsAsync(CancellationToken cancellationToken = default);
    Task<GetSponsorDTO> UpdateSponsorAsync(int id, UpdateSponsorDTO sponsorDTO);
}
