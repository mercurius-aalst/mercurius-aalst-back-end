using MercuriusAPI.Exceptions;
using MercuriusAPI.Services.Images;
using System.Reflection.Metadata.Ecma335;

namespace MercuriusAPI.Services.Files.ImageDecorator
{
    public class ImageFileService : IFileService
    {
        private readonly IFileService _innerService;
        private readonly int _maxFileSize;

        public ImageFileService(IFileService innerService, IConfiguration configuration)
        {
            _innerService = innerService;
            _maxFileSize = configuration.GetSection("AzureBlobStorage:MaxFileSizeMB").Get<int>() * 1024 * 1024;

        }
        public Task DeleteFileAsync<T>(string fileUrl) => _innerService.DeleteFileAsync<T>(fileUrl);
        public async Task<string> UploadFileAsync<T>(IFormFile file)
        {
            // Check if the file is null
            if(file == null || file.Length == 0)
            {
                throw new ValidationException("File is null or empty.");
            }

            if(file.Length > _maxFileSize)
            {
                throw new ValidationException("File size exceeds the 5MB limit.");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var fileExtension = Path.GetExtension(file.FileName)?.ToLowerInvariant();

            if(string.IsNullOrEmpty(fileExtension) || !allowedExtensions.Contains(fileExtension))
            {
                throw new ValidationException("Invalid file type. Only jpg, jpeg, and png are allowed.");
            }

            return await _innerService.UploadFileAsync<T>(file);
        }
    }
}
