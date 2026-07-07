using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Identity.Contracts;

public sealed record UserAnonymizedIntegrationEvent(
    UserId UserId,
    DateTime DeletedAtUtc);
