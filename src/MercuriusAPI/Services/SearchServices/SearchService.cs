using System.Text.Json;
using Mercurius.LAN.API.Data;
using Mercurius.LAN.API.DTOs.SearchDTOs;
using Mercurius.LAN.API.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Mercurius.LAN.API.Services.SearchServices;

public sealed class SearchService : ISearchService
{
    private readonly MercuriusDBContext _dbContext;

    public SearchService(MercuriusDBContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SearchResponseDTO> SearchAsync(string? query, string? cursor, int pageSize, CancellationToken cancellationToken = default)
    {
        var normalizedQuery = NormalizeQuery(query);
        if (normalizedQuery.Length > SearchRequestLimits.MaximumQueryLength)
            throw new ValidationException($"Query cannot exceed {SearchRequestLimits.MaximumQueryLength} characters.");

        var boundedPageSize = Math.Clamp(pageSize, 1, SearchRequestLimits.MaximumPageSize);
        if (normalizedQuery.Length < SearchRequestLimits.MinimumQueryLength)
            return new SearchResponseDTO { Results = [], HasMore = false };

        var decodedCursor = DecodeCursor(cursor, normalizedQuery);
        var pagedCandidates = await BuildPagedCandidateQuery(normalizedQuery, decodedCursor, boundedPageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = pagedCandidates.Count > boundedPageSize;
        if (hasMore)
            pagedCandidates.RemoveAt(pagedCandidates.Count - 1);

        return new SearchResponseDTO
        {
            Results = pagedCandidates.Select(ToResult).ToList(),
            NextCursor = hasMore ? BuildCursor(normalizedQuery, pagedCandidates[^1]) : null,
            HasMore = hasMore
        };
    }

    private IQueryable<SearchCandidate> BuildCandidateQuery(string normalizedQuery)
    {
        var containsPattern = $"%{EscapeLikePattern(normalizedQuery)}%";
        var prefixPattern = $"{EscapeLikePattern(normalizedQuery)}%";

        var users = _dbContext.Users
            .AsNoTracking()
            .Where(user =>
                !user.IsDeleted &&
                !string.IsNullOrEmpty(user.Username) &&
                !string.IsNullOrEmpty(user.NormalizedUsername) &&
                !string.IsNullOrWhiteSpace(user.Firstname) &&
                !string.IsNullOrWhiteSpace(user.Lastname) &&
                EF.Functions.Like(user.NormalizedUsername, containsPattern, "\\"))
            .Select(user => new SearchCandidate
            {
                RelevanceRank = user.NormalizedUsername == normalizedQuery
                    ? 0
                    : EF.Functions.Like(user.NormalizedUsername, prefixPattern, "\\") ? 1 : 2,
                NormalizedLabel = user.NormalizedUsername!,
                DisplayLabel = user.Username!,
                TypeOrder = 0,
                StableId = user.Id.ToString(),
                Type = "user",
                Username = user.Username,
                TeamName = null,
                GameId = null
            });

        var teams = _dbContext.Teams
            .AsNoTracking()
            .Where(team =>
                !string.IsNullOrEmpty(team.Name) &&
                !string.IsNullOrEmpty(team.NormalizedName) &&
                EF.Functions.Like(team.NormalizedName, containsPattern, "\\"))
            .Select(team => new SearchCandidate
            {
                RelevanceRank = team.NormalizedName == normalizedQuery
                    ? 0
                    : EF.Functions.Like(team.NormalizedName, prefixPattern, "\\") ? 1 : 2,
                NormalizedLabel = team.NormalizedName,
                DisplayLabel = team.Name,
                TypeOrder = 1,
                StableId = team.Id.ToString(),
                Type = "team",
                Username = null,
                TeamName = team.Name,
                GameId = null
            });

        var games = _dbContext.Games
            .AsNoTracking()
            .Where(game => !string.IsNullOrEmpty(game.Name) && EF.Functions.Like(game.Name.ToLower(), containsPattern, "\\"))
            .Select(game => new SearchCandidate
            {
                RelevanceRank = game.Name.ToLower() == normalizedQuery
                    ? 0
                    : EF.Functions.Like(game.Name.ToLower(), prefixPattern, "\\") ? 1 : 2,
                NormalizedLabel = game.Name.ToLower(),
                DisplayLabel = game.Name,
                TypeOrder = 2,
                StableId = game.Id.ToString(),
                Type = "game",
                Username = null,
                TeamName = null,
                GameId = game.Id
            });

        return users.Concat(teams).Concat(games);
    }

    private IQueryable<SearchCandidate> BuildPagedCandidateQuery(string normalizedQuery, SearchCursor? cursor, int limit)
    {
        return ApplyCursor(BuildCandidateQuery(normalizedQuery), cursor)
            .OrderBy(candidate => candidate.RelevanceRank)
            .ThenBy(candidate => candidate.NormalizedLabel)
            .ThenBy(candidate => candidate.TypeOrder)
            .ThenBy(candidate => candidate.StableId)
            .Take(limit);
    }

    private static IQueryable<SearchCandidate> ApplyCursor(IQueryable<SearchCandidate> candidates, SearchCursor? cursor)
    {
        if (cursor is null)
            return candidates;

        return candidates.Where(candidate =>
            (candidate.RelevanceRank > cursor.RelevanceRank) ||
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

    private static string NormalizeQuery(string? query)
    {
        return (query ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }

    private static SearchResultDTO ToResult(SearchCandidate candidate)
    {
        return new SearchResultDTO
        {
            Type = candidate.Type,
            DisplayLabel = candidate.DisplayLabel,
            SupportingText = candidate.Type switch
            {
                "user" => "User",
                "team" => "Team",
                "game" => "Game",
                _ => throw new InvalidOperationException($"Unsupported search result type '{candidate.Type}'.")
            },
            Username = candidate.Username,
            TeamName = candidate.TeamName,
            GameId = candidate.GameId
        };
    }

    private static string BuildCursor(string normalizedQuery, SearchCandidate candidate)
    {
        var payload = new SearchCursor(normalizedQuery, candidate.RelevanceRank, candidate.NormalizedLabel, candidate.TypeOrder, candidate.StableId);
        return Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(payload));
    }

    private static SearchCursor? DecodeCursor(string? cursor, string normalizedQuery)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return null;

        if (cursor.Length > SearchRequestLimits.MaximumCursorLength)
            throw new ValidationException("Cursor is invalid.");

        try
        {
            var payload = JsonSerializer.Deserialize<SearchCursor>(Convert.FromBase64String(cursor));
            if (payload is null ||
                string.IsNullOrEmpty(payload.Query) ||
                payload.RelevanceRank is < 0 or > 2 ||
                string.IsNullOrEmpty(payload.NormalizedLabel) ||
                payload.TypeOrder is < 0 or > 2 ||
                !Guid.TryParse(payload.StableId, out _))
                throw new ValidationException("Cursor is invalid.");

            if (!string.Equals(payload.Query, normalizedQuery, StringComparison.Ordinal))
                throw new ValidationException("Cursor does not match query.");

            return payload;
        }
        catch (Exception exception) when (exception is FormatException or JsonException)
        {
            throw new ValidationException("Cursor is invalid.");
        }
    }

    private sealed class SearchCandidate
    {
        public int RelevanceRank { get; init; }
        public required string NormalizedLabel { get; init; }
        public required string DisplayLabel { get; init; }
        public int TypeOrder { get; init; }
        public required string StableId { get; init; }
        public required string Type { get; init; }
        public string? Username { get; init; }
        public string? TeamName { get; init; }
        public Guid? GameId { get; init; }
    }

    private sealed record SearchCursor(string Query, int RelevanceRank, string NormalizedLabel, int TypeOrder, string StableId);
}
