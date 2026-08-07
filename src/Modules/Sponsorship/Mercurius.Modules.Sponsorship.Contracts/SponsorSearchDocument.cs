using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Sponsorship.Contracts;

public sealed record SponsorSearchDocument(
    SponsorId SponsorId,
    string Name,
    string? LogoUrl);
