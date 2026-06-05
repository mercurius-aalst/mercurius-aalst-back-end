using Imageflow.Fluent;

namespace Mercurius.LAN.API.Services.Files;


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
        if (string.IsNullOrWhiteSpace(folderPath))
            throw new InvalidOperationException("File storage location is not configured.");

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

    public Task DeleteImageAsync(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return Task.CompletedTask;

        var normalizedUrl = imageUrl.Replace("\\", "/", StringComparison.Ordinal);
        if (!normalizedUrl.StartsWith("images/", StringComparison.Ordinal) ||
            normalizedUrl.Contains("..", StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        var folderPath = _configuration["FileStorage:Location"];
        if (string.IsNullOrWhiteSpace(folderPath))
            return Task.CompletedTask;

        var fileName = Path.GetFileName(normalizedUrl);
        var filePath = Path.GetFullPath(Path.Combine(folderPath, fileName));
        var storageRoot = Path.GetFullPath(folderPath);
        if (!filePath.StartsWith(storageRoot, StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        if (File.Exists(filePath))
            File.Delete(filePath);

        return Task.CompletedTask;
    }
}
