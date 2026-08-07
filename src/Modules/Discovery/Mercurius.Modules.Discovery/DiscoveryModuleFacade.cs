using Mercurius.Modules.Discovery.Application;
using Mercurius.Modules.Discovery.Contracts;
using Mercurius.Modules.Discovery.Domain;
using Mercurius.Modules.Discovery.Infrastructure;
using Mercurius.Modules.Shared.Search;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.Modules.Discovery;

internal sealed class DiscoveryModuleFacade : IDiscoveryModule
{
    private readonly IDiscoveryDbContext _dbContext;
    private readonly SearchIndexRebuildService _rebuildService;

    public DiscoveryModuleFacade(
        IDiscoveryDbContext dbContext,
        SearchIndexRebuildService rebuildService)
    {
        _dbContext = dbContext;
        _rebuildService = rebuildService;
    }

    public async Task<DiscoverySearchResponse> SearchAsync(
        DiscoverySearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = SearchRequest.NormalizeQuery(request.Query);
        SearchRequest.ValidateQueryLength(normalizedQuery);

        var pageSize = SearchRequest.BoundPageSize(request.PageSize);
        if (normalizedQuery.Length < SearchRequestLimits.MinimumQueryLength)
            return new DiscoverySearchResponse([], null, false);

        var cursor = DecodeCursor(request.Cursor, normalizedQuery);
        var candidates = await BuildPagedCandidateQuery(normalizedQuery, cursor, pageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = candidates.Count > pageSize;
        if (hasMore)
            candidates.RemoveAt(candidates.Count - 1);

        return new DiscoverySearchResponse(
            candidates.Select(ToResult).ToList(),
            hasMore ? BuildCursor(normalizedQuery, candidates[^1]) : null,
            hasMore);
    }

    public Task<DiscoverySearchIndexRebuildJob> CreateSearchIndexRebuildJobAsync(
        CancellationToken cancellationToken = default) =>
        _rebuildService.CreateJobAsync(cancellationToken);

    public Task<DiscoverySearchIndexRebuildJob?> GetSearchIndexRebuildJobAsync(
        Guid jobId,
        CancellationToken cancellationToken = default) =>
        _rebuildService.GetJobAsync(jobId, cancellationToken);

    private IQueryable<SearchCandidate> BuildPagedCandidateQuery(
        string normalizedQuery,
        SearchCursor? cursor,
        int limit)
    {
        var escapedQuery = SearchRequest.EscapeLikePattern(normalizedQuery);
        var containsPattern = $"%{escapedQuery}%";
        var prefixPattern = $"{escapedQuery}%";

        var candidates = _dbContext.SearchDocuments
            .AsNoTracking()
            .Where(document =>
                !document.IsDeleted &&
                (document.EntityType == SearchDocumentTypes.User ||
                 document.EntityType == SearchDocumentTypes.Team ||
                 document.EntityType == SearchDocumentTypes.Game) &&
                EF.Functions.Like(document.NormalizedText, containsPattern, "\\"))
            .Select(document => new SearchCandidate
            {
                RelevanceRank = document.NormalizedText == normalizedQuery
                    ? 0
                    : EF.Functions.Like(document.NormalizedText, prefixPattern, "\\") ? 1 : 2,
                NormalizedLabel = document.NormalizedText,
                TypeOrder = document.EntityType == SearchDocumentTypes.User
                    ? 0
                    : document.EntityType == SearchDocumentTypes.Team ? 1 : 2,
                StableId = document.EntityId,
                Type = document.EntityType,
                DisplayLabel = document.Title
            });

        if (cursor is not null)
        {
            candidates = candidates.Where(candidate =>
                candidate.RelevanceRank > cursor.RelevanceRank ||
                (candidate.RelevanceRank == cursor.RelevanceRank &&
                 string.Compare(candidate.NormalizedLabel, cursor.NormalizedLabel) > 0) ||
                (candidate.RelevanceRank == cursor.RelevanceRank &&
                 candidate.NormalizedLabel == cursor.NormalizedLabel &&
                 candidate.TypeOrder > cursor.TypeOrder) ||
                (candidate.RelevanceRank == cursor.RelevanceRank &&
                 candidate.NormalizedLabel == cursor.NormalizedLabel &&
                 candidate.TypeOrder == cursor.TypeOrder &&
                 string.Compare(candidate.StableId, cursor.StableId) > 0));
        }

        return candidates
            .OrderBy(candidate => candidate.RelevanceRank)
            .ThenBy(candidate => candidate.NormalizedLabel)
            .ThenBy(candidate => candidate.TypeOrder)
            .ThenBy(candidate => candidate.StableId)
            .Take(limit);
    }

    private static DiscoverySearchResult ToResult(SearchCandidate candidate)
    {
        return candidate.Type switch
        {
            SearchDocumentTypes.User => new DiscoverySearchResult(
                candidate.Type,
                candidate.DisplayLabel,
                "User",
                candidate.DisplayLabel,
                null,
                null),
            SearchDocumentTypes.Team => new DiscoverySearchResult(
                candidate.Type,
                candidate.DisplayLabel,
                "Team",
                null,
                candidate.DisplayLabel,
                null),
            SearchDocumentTypes.Game when Guid.TryParse(candidate.StableId, out var gameId) => new DiscoverySearchResult(
                candidate.Type,
                candidate.DisplayLabel,
                "Game",
                null,
                null,
                gameId),
            _ => throw new InvalidOperationException($"Unsupported search document type '{candidate.Type}'.")
        };
    }

    private static string BuildCursor(string normalizedQuery, SearchCandidate candidate)
    {
        return SearchCursorCodec.Encode(new SearchCursor(
            normalizedQuery,
            candidate.RelevanceRank,
            candidate.NormalizedLabel,
            candidate.TypeOrder,
            candidate.StableId));
    }

    private static SearchCursor? DecodeCursor(string? cursor, string normalizedQuery)
    {
        return SearchCursorCodec.Decode<SearchCursor>(
            cursor,
            normalizedQuery,
            payload =>
                !string.IsNullOrEmpty(payload.Query) &&
                payload.RelevanceRank is >= 0 and <= 2 &&
                !string.IsNullOrEmpty(payload.NormalizedLabel) &&
                payload.TypeOrder is >= 0 and <= 2 &&
                Guid.TryParse(payload.StableId, out _),
            payload => payload.Query);
    }

    private sealed class SearchCandidate
    {
        public int RelevanceRank { get; init; }
        public required string NormalizedLabel { get; init; }
        public int TypeOrder { get; init; }
        public required string StableId { get; init; }
        public required string Type { get; init; }
        public required string DisplayLabel { get; init; }
    }

    private sealed record SearchCursor(
        string Query,
        int RelevanceRank,
        string NormalizedLabel,
        int TypeOrder,
        string StableId);
}
