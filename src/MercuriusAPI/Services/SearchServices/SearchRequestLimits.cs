namespace Mercurius.LAN.API.Services.SearchServices;

public static class SearchRequestLimits
{
    public const int MinimumQueryLength = 3;
    public const int MaximumQueryLength = 100;
    public const int MaximumCursorLength = 512;
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 50;
}
