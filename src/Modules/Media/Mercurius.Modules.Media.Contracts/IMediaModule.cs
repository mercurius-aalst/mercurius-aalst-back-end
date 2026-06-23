namespace Mercurius.Modules.Media.Contracts;

public interface IMediaModule
{
    Task<StoredMediaAsset> SaveImageAsync(
        MediaUpload upload,
        CancellationToken cancellationToken = default);

    Task DeleteImageAsync(
        string? mediaUrl,
        CancellationToken cancellationToken = default);
}
