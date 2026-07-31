using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Sponsorship.Contracts;

public sealed record SponsorPlacementInput(
    SponsorId SponsorId,
    SponsorContext Context,
    string? Headline,
    string? SupportLine,
    int DisplayOrder);
