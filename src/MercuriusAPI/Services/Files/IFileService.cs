
namespace MercuriusAPI.Services.Images
{
    public interface IFileService
    {
        Task DeleteFileAsync<T>(string fileUrl);
        Task<string> UploadFileAsync<T>(IFormFile file);
    }
}