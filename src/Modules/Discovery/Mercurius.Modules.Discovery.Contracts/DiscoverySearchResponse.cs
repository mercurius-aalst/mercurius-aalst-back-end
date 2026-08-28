namespace Mercurius.Modules.Discovery.Contracts;

public sealed record DiscoverySearchResponse(
    IReadOnlyList<DiscoverySearchResult> Results,
    string? NextCursor,
    bool HasMore);
