using Microsoft.AspNetCore.Http;

namespace Mercurius.Modules.Teams.Services;

internal interface ITeamLogoStorage
{
    Task<string> SaveImageAsync(IFormFile image, CancellationToken cancellationToken = default);

    Task DeleteImageAsync(string? imageUrl, CancellationToken cancellationToken = default);
}
