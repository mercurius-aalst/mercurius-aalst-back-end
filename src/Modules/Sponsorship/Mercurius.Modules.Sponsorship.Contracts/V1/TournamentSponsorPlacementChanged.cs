using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Sponsorship.Contracts.V1;

public sealed record TournamentSponsorPlacementChanged(
    TournamentId TournamentId,
    SponsorPlacementId? PlacementId,
    SponsorId? SponsorId,
    SponsorContext? Context,
    string? Headline,
    string? SupportLine,
    int? DisplayOrder);
