using Mercurius.Modules.Discovery.Domain;
using Mercurius.Modules.Discovery.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Platform.Eventing;

namespace Mercurius.Modules.Discovery.Application;

internal sealed class SearchDocumentProjector
{
    private readonly IDiscoveryDbContext _dbContext;

    public SearchDocumentProjector(IDiscoveryDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task UpsertAsync(
        string entityType,
        string entityId,
        string title,
        string subtitle,
        string? imageUrl,
        string route,
        long sourceVersion,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken)
    {
        var document = await GetDocumentAsync(entityType, entityId, cancellationToken);
        if (document is not null && ProjectionVersionGuard.IsStale(sourceVersion, document.SourceVersion))
            return;

        if (document is null)
        {
            document = new SearchDocument
            {
                EntityType = entityType,
                EntityId = entityId
            };
            _dbContext.SearchDocuments.Add(document);
        }

        document.Title = title;
        document.Subtitle = subtitle;
        document.ImageUrl = imageUrl;
        document.Route = route;
        document.NormalizedText = title.Trim().ToLowerInvariant();
        document.TypeOrder = SearchDocumentTypes.GetTypeOrder(entityType);
        document.SourceVersion = sourceVersion;
        document.IsDeleted = false;
        document.UpdatedAtUtc = updatedAtUtc;

    }

    public async Task MarkDeletedAsync(
        string entityType,
        string entityId,
        long sourceVersion,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken)
    {
        var document = await GetDocumentAsync(entityType, entityId, cancellationToken);
        if (document is not null && ProjectionVersionGuard.IsStale(sourceVersion, document.SourceVersion))
            return;

        if (document is null)
        {
            document = new SearchDocument
            {
                EntityType = entityType,
                EntityId = entityId
            };
            _dbContext.SearchDocuments.Add(document);
        }

        document.Title = string.Empty;
        document.Subtitle = string.Empty;
        document.ImageUrl = null;
        document.Route = string.Empty;
        document.NormalizedText = string.Empty;
        document.TypeOrder = SearchDocumentTypes.GetTypeOrder(entityType);
        document.SourceVersion = sourceVersion;
        document.IsDeleted = true;
        document.UpdatedAtUtc = updatedAtUtc;

    }

    private Task<SearchDocument?> GetDocumentAsync(
        string entityType,
        string entityId,
        CancellationToken cancellationToken)
    {
        var trackedDocument = _dbContext.SearchDocuments.Local.SingleOrDefault(
            candidate => candidate.EntityType == entityType && candidate.EntityId == entityId);
        return trackedDocument is not null
            ? Task.FromResult<SearchDocument?>(trackedDocument)
            : _dbContext.SearchDocuments.SingleOrDefaultAsync(
                candidate => candidate.EntityType == entityType && candidate.EntityId == entityId,
                cancellationToken);
    }
}
