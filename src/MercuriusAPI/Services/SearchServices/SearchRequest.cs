using Mercurius.LAN.API.Exceptions;

namespace Mercurius.LAN.API.Services.SearchServices;

public static class SearchRequest
{
    public static string NormalizeQuery(string? query)
    {
        return (query ?? string.Empty).Trim().ToLowerInvariant();
    }

    public static void ValidateQueryLength(string normalizedQuery)
    {
        if (normalizedQuery.Length > SearchRequestLimits.MaximumQueryLength)
            throw new ValidationException($"Query cannot exceed {SearchRequestLimits.MaximumQueryLength} characters.");
    }

    public static void ValidatePageSize(int? pageSize)
    {
        if (pageSize is <= 0)
            throw new ValidationException("pageSize must be greater than 0.");
    }

    public static int BoundPageSize(int? pageSize)
    {
        return Math.Min(pageSize ?? SearchRequestLimits.DefaultPageSize, SearchRequestLimits.MaximumPageSize);
    }

    public static int BoundPageSize(int pageSize)
    {
        return Math.Clamp(pageSize, 1, SearchRequestLimits.MaximumPageSize);
    }

    public static string EscapeLikePattern(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}
