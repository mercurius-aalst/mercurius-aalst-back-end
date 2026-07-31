namespace Mercurius.LAN.API.Services.Files;

public interface IFileService
{
    Task<string> SaveImageAsync(IFormFile image, CancellationToken cancellationToken = default);
    Task DeleteImageAsync(string? imageUrl);
}
