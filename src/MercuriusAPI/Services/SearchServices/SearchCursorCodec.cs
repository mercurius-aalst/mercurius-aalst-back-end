using System.Text.Json;
using Mercurius.LAN.API.Exceptions;

namespace Mercurius.LAN.API.Services.SearchServices;

public static class SearchCursorCodec
{
    public static string Encode<TCursor>(TCursor cursor)
    {
        return Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(cursor));
    }

    public static TCursor? Decode<TCursor>(
        string? cursor,
        string normalizedQuery,
        Func<TCursor, bool> isValid,
        Func<TCursor, string> getQuery)
        where TCursor : class
    {
        if (string.IsNullOrWhiteSpace(cursor))
            return null;

        if (cursor.Length > SearchRequestLimits.MaximumCursorLength)
            throw new ValidationException("Cursor is invalid.");

        try
        {
            var payload = JsonSerializer.Deserialize<TCursor>(Convert.FromBase64String(cursor));
            if (payload is null || !isValid(payload))
                throw new ValidationException("Cursor is invalid.");

            if (!string.Equals(getQuery(payload), normalizedQuery, StringComparison.Ordinal))
                throw new ValidationException("Cursor does not match query.");

            return payload;
        }
        catch (Exception exception) when (exception is FormatException or JsonException)
        {
            throw new ValidationException("Cursor is invalid.");
        }
    }
}
