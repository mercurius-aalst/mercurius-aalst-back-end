using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Sponsorship.Contracts;

public sealed record GameSponsorPlacementChangedV1(
    GameId GameId,
    SponsorPlacementId? PlacementId,
    SponsorId? SponsorId,
    SponsorContext? Context,
    string? Headline,
    string? SupportLine,
    int? DisplayOrder);
