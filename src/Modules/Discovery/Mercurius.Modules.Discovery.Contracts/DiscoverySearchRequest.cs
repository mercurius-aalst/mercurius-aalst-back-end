namespace Mercurius.Modules.Discovery.Contracts;

public sealed record DiscoverySearchRequest(
    string? Query,
    string? Cursor,
    int PageSize);
