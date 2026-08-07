using Mercurius.Modules.Sponsorship.Application.DTOs;

namespace Mercurius.Modules.Sponsorship.Application.Services;

internal interface ISponsorService
{
    Task<GetSponsorDTO> CreateSponsorAsync(CreateSponsorDTO sponsorDTO, CancellationToken cancellationToken = default);
    Task DeleteSponsorAsync(int id, CancellationToken cancellationToken = default);
    Task<GetSponsorDTO> GetSponsorByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GetSponsorDTO>> GetSponsorsAsync(CancellationToken cancellationToken = default);
    Task<GetSponsorDTO> UpdateSponsorAsync(int id, UpdateSponsorDTO sponsorDTO, CancellationToken cancellationToken = default);
}
