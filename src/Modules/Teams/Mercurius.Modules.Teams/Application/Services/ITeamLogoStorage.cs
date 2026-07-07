using Microsoft.AspNetCore.Http;

namespace Mercurius.Modules.Teams.Services;

internal interface ITeamLogoStorage
{
    Task<string> SaveImageAsync(IFormFile image);

    Task DeleteImageAsync(string? imageUrl);
}
