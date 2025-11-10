namespace MercuriusAPI.Services.Files;

public interface IFileService
{
    Task<string> SaveImageAsync(IFormFile image);
}