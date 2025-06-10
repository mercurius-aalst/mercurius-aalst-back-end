
namespace MercuriusAPI.Services.Images
{
    public interface IImageService
    {
        Task DeleteFileAsync(string fileUrl);
        Task<string> UploadFileAsync(IFormFile file);
    }
}