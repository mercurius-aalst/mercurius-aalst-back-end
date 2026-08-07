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
        document.SourceVersion = sourceVersion;
        document.IsDeleted = true;
        document.UpdatedAtUtc = updatedAtUtc;

    }

    public async Task MarkMissingAsDeletedAsync(
        string entityType,
        IReadOnlyCollection<string> sourceEntityIds,
        long sourceVersion,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken)
    {
        var documents = await _dbContext.SearchDocuments
            .Where(document =>
                document.EntityType == entityType &&
                !document.IsDeleted &&
                document.SourceVersion <= sourceVersion &&
                !sourceEntityIds.Contains(document.EntityId))
            .ToListAsync(cancellationToken);

        foreach (var document in documents)
        {
            document.Title = string.Empty;
            document.Subtitle = string.Empty;
            document.ImageUrl = null;
            document.Route = string.Empty;
            document.NormalizedText = string.Empty;
            document.SourceVersion = sourceVersion;
            document.IsDeleted = true;
            document.UpdatedAtUtc = updatedAtUtc;
        }
    }

    public async Task RebuildAsync(
        string entityType,
        IReadOnlyCollection<SearchDocumentProjection> projections,
        long sourceVersion,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken)
    {
        var documents = await _dbContext.SearchDocuments
            .Where(document => document.EntityType == entityType)
            .ToListAsync(cancellationToken);
        var documentsByEntityId = documents.ToDictionary(document => document.EntityId);
        var sourceEntityIds = projections.Select(projection => projection.EntityId).ToHashSet();

        foreach (var projection in projections)
        {
            if (!documentsByEntityId.TryGetValue(projection.EntityId, out var document))
            {
                document = new SearchDocument
                {
                    EntityType = entityType,
                    EntityId = projection.EntityId
                };
                _dbContext.SearchDocuments.Add(document);
            }
            else if (ProjectionVersionGuard.IsStale(sourceVersion, document.SourceVersion))
            {
                continue;
            }

            document.Title = projection.Title;
            document.Subtitle = projection.Subtitle;
            document.ImageUrl = projection.ImageUrl;
            document.Route = projection.Route;
            document.NormalizedText = projection.Title.Trim().ToLowerInvariant();
            document.SourceVersion = sourceVersion;
            document.IsDeleted = false;
            document.UpdatedAtUtc = updatedAtUtc;
        }

        foreach (var document in documents)
        {
            if (document.IsDeleted ||
                document.SourceVersion > sourceVersion ||
                sourceEntityIds.Contains(document.EntityId))
            {
                continue;
            }

            document.Title = string.Empty;
            document.Subtitle = string.Empty;
            document.ImageUrl = null;
            document.Route = string.Empty;
            document.NormalizedText = string.Empty;
            document.SourceVersion = sourceVersion;
            document.IsDeleted = true;
            document.UpdatedAtUtc = updatedAtUtc;
        }
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
