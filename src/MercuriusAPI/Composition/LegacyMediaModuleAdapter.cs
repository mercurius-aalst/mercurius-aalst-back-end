using Mercurius.LAN.API.Services.Files;
using Mercurius.Modules.Media.Contracts;
using Microsoft.AspNetCore.Http;

namespace Mercurius.LAN.API.Composition;

internal sealed class LegacyMediaModuleAdapter : IMediaModule
{
    private readonly IFileService _fileService;

    public LegacyMediaModuleAdapter(IFileService fileService)
    {
        _fileService = fileService;
    }

    public async Task<StoredMediaAsset> SaveImageAsync(
        MediaUpload upload,
        CancellationToken cancellationToken = default)
    {
        var formFile = new FormFile(upload.Content, 0, upload.Length, "image", upload.FileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = upload.ContentType
        };
        var url = await _fileService.SaveImageAsync(formFile, cancellationToken);
        return new StoredMediaAsset(url);
    }

    public Task DeleteImageAsync(
        string? mediaUrl,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _fileService.DeleteImageAsync(mediaUrl);
    }
}
