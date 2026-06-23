using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Sponsorship.Contracts;

public sealed record SponsorPlacementSummary(
    SponsorPlacementId Id,
    GameId GameId,
    SponsorSummary Sponsor,
    SponsorContext Context,
    string? Headline,
    string? SupportLine,
    int DisplayOrder);
