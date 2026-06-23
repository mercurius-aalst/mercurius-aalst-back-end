using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Discovery.Contracts;

public sealed record DiscoverySearchResult(
    DiscoverySearchResultType Type,
    string DisplayLabel,
    string SupportingText,
    string? Username,
    string? TeamName,
    GameId? GameId);
