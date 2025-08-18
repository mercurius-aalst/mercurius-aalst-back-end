using Imageflow.Fluent;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace MercuriusAPI.Services.Files
{
    public class FileService : IFileService
    {
        public async Task<string> SaveImageAsync(IFormFile image)
        {
            var folderPath = Path.Combine("wwwroot", "images");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var fileName = Path.GetRandomFileName() + ".webp"; // Save as .webp
            var filePath = Path.Combine(folderPath, fileName);

            using (var inputStream = image.OpenReadStream())
            {
                if (inputStream.Length == 0)
                {
                    throw new InvalidOperationException("Uploaded file is empty.");
                }

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