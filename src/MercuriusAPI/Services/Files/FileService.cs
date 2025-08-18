using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace MercuriusAPI.Services.Files
{
    public class FileService : IFileService
    {
        public async Task<string> SaveImageAsync(IFormFile image)
        {
            var folderPath = Path.Combine("wwwroot", "Images");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var fileName = Path.GetRandomFileName() + Path.GetExtension(image.FileName);
            var filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            return Path.Combine("Images", fileName).Replace("\\", "/"); // Return relative path
        }
    }
}