using Imageflow.Fluent;
using MercuriusAPI.Exceptions;

namespace MercuriusAPI.Services.Files
{

    public class FileService : IFileService
    {
        private readonly IConfiguration _configuration;
        private readonly int _maxFileSizeInMB;

        public FileService(IConfiguration configuration)
        {
            _configuration = configuration;
            _maxFileSizeInMB = _configuration.GetValue<int>("FileStorage:MaxFileSizeInMB");
        }
        public async Task<string> SaveImageAsync(IFormFile image)
        {
            if(image is null)
                throw new ValidationException("No file provided");
            if(image.Length == 0)
                throw new ValidationException("Empty file provided");
            if(image.Length > _maxFileSizeInMB * 1024 * 1024)
                throw new ValidationException($"File too big, maximum file size is {_maxFileSizeInMB}MB");

            var folderPath = _configuration["FileStorage:Location"];
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var fileName = Path.GetRandomFileName() + ".webp"; // Save as .webp
            var filePath = Path.Combine(folderPath, fileName);

            using (var inputStream = image.OpenReadStream())
            {

                inputStream.Position = 0; // Reset stream position

                using (var outputStream = new FileStream(filePath, FileMode.Create))
                {
                    await new ImageJob()
                        .Decode(inputStream, true) // Decode the input image
                        .Encode(new StreamDestination(outputStream, true), new WebPLosslessEncoder())
                        .Finish().InProcessAsync();
                }
            }

            return Path.Combine("images", fileName).Replace("\\", "/"); // Return relative path
        }
    }
}