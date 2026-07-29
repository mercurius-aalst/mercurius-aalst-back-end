using Imageflow.Fluent;
using Mercurius.Modules.Shared.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Mercurius.Modules.Teams.Services;

internal sealed class TeamLogoStorage : ITeamLogoStorage
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    };

    private readonly IConfiguration _configuration;
    private readonly int _maxFileSizeInMB;

    public TeamLogoStorage(IConfiguration configuration)
    {
        _configuration = configuration;
        _maxFileSizeInMB = _configuration.GetValue<int>("FileStorage:MaxFileSizeInMB");
    }

    public async Task<string> SaveImageAsync(IFormFile image, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateImage(image);

        var folderPath = _configuration["FileStorage:Location"];
        if (string.IsNullOrWhiteSpace(folderPath))
            throw new InvalidOperationException("File storage location is not configured.");

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        var fileName = Path.GetRandomFileName() + ".webp";
        var filePath = Path.Combine(folderPath, fileName);

        cancellationToken.ThrowIfCancellationRequested();
        await using var outputStream = new FileStream(filePath, FileMode.Create);
        await using var inputStream = image.OpenReadStream();
        inputStream.Position = 0;

        cancellationToken.ThrowIfCancellationRequested();
        await new ImageJob()
            .Decode(inputStream, true)
            .Encode(new StreamDestination(outputStream, true), new WebPLosslessEncoder())
            .Finish()
            .InProcessAsync();

        return Path.Combine("images", fileName).Replace("\\", "/", StringComparison.Ordinal);
    }

    public Task DeleteImageAsync(string? imageUrl, CancellationToken cancellationToken = default)
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

        cancellationToken.ThrowIfCancellationRequested();
        if (File.Exists(filePath))
            File.Delete(filePath);

        return Task.CompletedTask;
    }

    private void ValidateImage(IFormFile image)
    {
        if (image is null)
            throw new ValidationException("No file provided");
        if (image.Length == 0)
            throw new ValidationException("Empty file provided");
        if (image.Length > _maxFileSizeInMB * 1024 * 1024)
            throw new ValidationException($"File too big, maximum file size is {_maxFileSizeInMB}MB");
        if (string.IsNullOrWhiteSpace(image.ContentType) || !AllowedContentTypes.Contains(image.ContentType))
            throw new ValidationException("Unsupported image type.");
    }
}
