using Mercurius.Modules.Teams.DTOs;

namespace Mercurius.Modules.Teams.Services;

internal interface ITeamQueries
{
    Task<IEnumerable<GetTeamDTO>> GetAllTeamsAsync(CancellationToken cancellationToken = default);
    Task<GetTeamDTO> GetTeamByIdAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<PublicTeamProfileDTO> GetPublicTeamProfileAsync(string teamName, CancellationToken cancellationToken = default);
}
