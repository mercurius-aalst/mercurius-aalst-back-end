using Imageflow.Fluent;
using MercuriusAPI.Exceptions;

namespace MercuriusAPI.Services.Files
{

    public class FileService : IFileService
    {
        private readonly IConfiguration _configuration;

        public FileService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task<string> SaveImageAsync(IFormFile image)
        {            
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