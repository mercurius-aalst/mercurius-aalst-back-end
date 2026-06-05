namespace Mercurius.LAN.API.Services.Files;

public interface IFileService
{
    Task<string> SaveImageAsync(IFormFile image);
    Task DeleteImageAsync(string? imageUrl);
}
