using Mercurius.Modules.Teams.DTOs;
using Microsoft.AspNetCore.Http;

namespace Mercurius.Modules.Teams.Services;

internal interface ITeamLogoCommands
{
    Task<TeamLogoResponseDTO> UploadTeamLogoAsync(
        string auth0UserId,
        Guid teamId,
        IFormFile logo,
        CancellationToken cancellationToken = default);

    Task<TeamLogoResponseDTO> RemoveTeamLogoAsync(
        string auth0UserId,
        Guid teamId,
        CancellationToken cancellationToken = default);
}
