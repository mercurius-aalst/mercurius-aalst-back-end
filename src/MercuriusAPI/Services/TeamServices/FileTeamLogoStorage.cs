using Mercurius.LAN.API.Services.Files;
using Mercurius.Modules.Teams.Services;

namespace Mercurius.LAN.API.Services.TeamServices;

public sealed class FileTeamLogoStorage : ITeamLogoStorage
{
    private readonly IFileService _fileService;

    public FileTeamLogoStorage(IFileService fileService)
    {
        _fileService = fileService;
    }

    public Task<string> SaveImageAsync(IFormFile image)
    {
        return _fileService.SaveImageAsync(image);
    }

    public Task DeleteImageAsync(string? imageUrl)
    {
        return _fileService.DeleteImageAsync(imageUrl);
    }
}
