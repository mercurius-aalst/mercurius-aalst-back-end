using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Sponsorship.Contracts;

public sealed record SponsorCreatedV1(
    SponsorId SponsorId,
    string Name,
    SponsorTier SponsorTier,
    string LogoUrl,
    string InfoUrl,
    string? Description);
