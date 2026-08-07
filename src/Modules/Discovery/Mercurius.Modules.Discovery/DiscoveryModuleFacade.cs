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
        var candidates = await GetPagedCandidatesAsync(
            normalizedQuery,
            cursor,
            pageSize + 1,
            cancellationToken);

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

    private async Task<List<SearchCandidate>> GetPagedCandidatesAsync(
        string normalizedQuery,
        SearchCursor? cursor,
        int limit,
        CancellationToken cancellationToken)
    {
        var escapedQuery = SearchRequest.EscapeLikePattern(normalizedQuery);
        var containsPattern = $"%{escapedQuery}%";
        var prefixPattern = $"{escapedQuery}%";
        var candidates = new List<SearchCandidate>(limit);
        var firstRank = cursor?.RelevanceRank ?? 0;

        for (var rank = firstRank; rank <= 2 && candidates.Count < limit; rank++)
        {
            var rankCandidates = BuildRankCandidateQuery(
                normalizedQuery,
                prefixPattern,
                containsPattern,
                rank);

            if (cursor is not null && rank == cursor.RelevanceRank)
            {
                rankCandidates = rankCandidates.Where(candidate =>
                    string.Compare(candidate.NormalizedLabel, cursor.NormalizedLabel) > 0 ||
                    (candidate.NormalizedLabel == cursor.NormalizedLabel &&
                     candidate.TypeOrder > cursor.TypeOrder) ||
                    (candidate.NormalizedLabel == cursor.NormalizedLabel &&
                     candidate.TypeOrder == cursor.TypeOrder &&
                     string.Compare(candidate.StableId, cursor.StableId) > 0));
            }

            rankCandidates = rankCandidates
                .OrderBy(candidate => candidate.NormalizedLabel)
                .ThenBy(candidate => candidate.TypeOrder)
                .ThenBy(candidate => candidate.StableId)
                .Take(limit - candidates.Count);

            candidates.AddRange(await rankCandidates.ToListAsync(cancellationToken));
        }

        return candidates;
    }

    private IQueryable<SearchCandidate> BuildRankCandidateQuery(
        string normalizedQuery,
        string prefixPattern,
        string containsPattern,
        int rank)
    {
        var documents = _dbContext.SearchDocuments
            .AsNoTracking()
            .Where(document =>
                !document.IsDeleted &&
                document.TypeOrder <= SearchDocumentTypes.GetTypeOrder(SearchDocumentTypes.Game));

        documents = rank switch
        {
            0 => documents.Where(document => document.NormalizedText == normalizedQuery),
            1 => documents.Where(document =>
                EF.Functions.Like(document.NormalizedText, prefixPattern, "\\") &&
                document.NormalizedText != normalizedQuery),
            2 => documents.Where(document =>
                EF.Functions.Like(document.NormalizedText, containsPattern, "\\") &&
                !EF.Functions.Like(document.NormalizedText, prefixPattern, "\\")),
            _ => throw new ArgumentOutOfRangeException(nameof(rank))
        };

        return documents
            .Select(document => new SearchCandidate
            {
                RelevanceRank = rank,
                NormalizedLabel = document.NormalizedText,
                TypeOrder = document.TypeOrder,
                StableId = document.EntityId,
                Type = document.EntityType,
                DisplayLabel = document.Title
            });
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
