using System.Text;
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
            return new SearchResponseDTO { Results = [], TotalCount = 0, HasMore = false };

        var offset = DecodeCursor(cursor, normalizedQuery);
        var orderedCandidates = await LoadCandidatesAsync(normalizedQuery, cancellationToken);
        if (offset > orderedCandidates.Count)
            throw new ValidationException("Cursor is out of range.");

        var pagedResults = orderedCandidates
            .Skip(offset)
            .Take(boundedPageSize)
            .Select(candidate => candidate.Result)
            .ToList();

        var nextOffset = offset + pagedResults.Count;
        var hasMore = nextOffset < orderedCandidates.Count;

        return new SearchResponseDTO
        {
            Results = pagedResults,
            NextCursor = hasMore ? BuildCursor(normalizedQuery, nextOffset) : null,
            TotalCount = orderedCandidates.Count,
            HasMore = hasMore
        };
    }

    private async Task<List<SearchCandidate>> LoadCandidatesAsync(string normalizedQuery, CancellationToken cancellationToken)
    {
        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(user =>
                !user.IsDeleted &&
                user.Username != null &&
                user.Username != string.Empty &&
                user.NormalizedUsername != null &&
                user.NormalizedUsername != string.Empty &&
                user.Firstname != null &&
                user.Firstname != string.Empty &&
                user.Lastname != null &&
                user.Lastname != string.Empty &&
                user.NormalizedUsername.Contains(normalizedQuery))
            .Select(user => new UserProjection(user.Id, user.Username!, user.NormalizedUsername!))
            .ToListAsync(cancellationToken);

        var teams = await _dbContext.Teams
            .AsNoTracking()
            .Where(team =>
                team.Name != null &&
                team.NormalizedName != null &&
                team.NormalizedName.Contains(normalizedQuery))
            .Select(team => new TeamProjection(team.Id, team.Name, team.NormalizedName))
            .ToListAsync(cancellationToken);

        var games = await _dbContext.Games
            .AsNoTracking()
            .Where(game => game.Name != null && game.Name.ToLower().Contains(normalizedQuery))
            .Select(game => new GameProjection(game.Id, game.Name, game.Name.ToLower()))
            .ToListAsync(cancellationToken);

        var candidates = new List<SearchCandidate>(users.Count + teams.Count + games.Count);

        candidates.AddRange(users.Select(user => new SearchCandidate(
            RelevanceRank: GetRelevanceRank(user.NormalizedUsername, normalizedQuery),
            DisplayLabel: user.Username,
            TypeOrder: 0,
            StableKey: $"{user.NormalizedUsername}|{user.Id:N}",
            Result: new SearchResultDTO
            {
                Type = "user",
                DisplayLabel = user.Username,
                SupportingText = "User",
                Username = user.Username
            })));

        candidates.AddRange(teams.Select(team => new SearchCandidate(
            RelevanceRank: GetRelevanceRank(team.NormalizedName, normalizedQuery),
            DisplayLabel: team.Name,
            TypeOrder: 1,
            StableKey: $"{team.NormalizedName}|{team.Id:N}",
            Result: new SearchResultDTO
            {
                Type = "team",
                DisplayLabel = team.Name,
                SupportingText = "Team",
                TeamName = team.Name
            })));

        candidates.AddRange(games.Select(game => new SearchCandidate(
            RelevanceRank: GetRelevanceRank(game.NormalizedName, normalizedQuery),
            DisplayLabel: game.Name,
            TypeOrder: 2,
            StableKey: $"{game.NormalizedName}|{game.Id:N}",
            Result: new SearchResultDTO
            {
                Type = "game",
                DisplayLabel = game.Name,
                SupportingText = "Game",
                GameId = game.Id
            })));

        return candidates
            .OrderBy(candidate => candidate.RelevanceRank)
            .ThenBy(candidate => candidate.DisplayLabel, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.TypeOrder)
            .ThenBy(candidate => candidate.StableKey, StringComparer.Ordinal)
            .ToList();
    }

    private static int GetRelevanceRank(string normalizedValue, string normalizedQuery)
    {
        if (normalizedValue == normalizedQuery)
            return 0;
        if (normalizedValue.StartsWith(normalizedQuery, StringComparison.Ordinal))
            return 1;
        return 2;
    }

    private static string NormalizeQuery(string? query)
    {
        return (query ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static string BuildCursor(string normalizedQuery, int offset)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{normalizedQuery}|{offset}"));
    }

    private static int DecodeCursor(string? cursor, string normalizedQuery)
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return 0;

        try
        {
            var payload = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var separatorIndex = payload.LastIndexOf('|');
            if (separatorIndex <= 0 || separatorIndex == payload.Length - 1)
                throw new ValidationException("Cursor is invalid.");

            var cursorQuery = payload[..separatorIndex];
            if (!string.Equals(cursorQuery, normalizedQuery, StringComparison.Ordinal))
                throw new ValidationException("Cursor does not match query.");

            var offsetSegment = payload[(separatorIndex + 1)..];
            if (!int.TryParse(offsetSegment, out var offset) || offset < 0)
                throw new ValidationException("Cursor is invalid.");

            return offset;
        }
        catch (FormatException)
        {
            throw new ValidationException("Cursor is invalid.");
        }
    }

    private sealed record SearchCandidate(int RelevanceRank, string DisplayLabel, int TypeOrder, string StableKey, SearchResultDTO Result);
    private sealed record UserProjection(Guid Id, string Username, string NormalizedUsername);
    private sealed record TeamProjection(Guid Id, string Name, string NormalizedName);
    private sealed record GameProjection(Guid Id, string Name, string NormalizedName);
}
