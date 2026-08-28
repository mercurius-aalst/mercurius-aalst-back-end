using Mercurius.Modules.Teams.DTOs;

namespace Mercurius.Modules.Teams.Services;

internal interface ITeamQueries
{
    Task<IReadOnlyList<GetTeamDTO>> GetAllTeamsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<GetTeamDTO> GetTeamByIdAsync(Guid teamId, CancellationToken cancellationToken = default);
    Task<PublicTeamProfileDTO> GetPublicTeamProfileAsync(string teamName, CancellationToken cancellationToken = default);
}
