using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Sponsorship.Contracts;

public sealed record SponsorPlacementSummary(
    SponsorPlacementId Id,
    TournamentId TournamentId,
    SponsorSummary Sponsor,
    SponsorContext Context,
    string? Headline,
    string? SupportLine,
    int DisplayOrder);
