using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Sponsorship.Contracts.V1;

public sealed record GameSponsorPlacementChanged(
    GameId GameId,
    SponsorPlacementId? PlacementId,
    SponsorId? SponsorId,
    SponsorContext? Context,
    string? Headline,
    string? SupportLine,
    int? DisplayOrder);
