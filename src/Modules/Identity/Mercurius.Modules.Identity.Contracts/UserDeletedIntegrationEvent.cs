using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Identity.Contracts;

public sealed record UserDeletedIntegrationEvent(
    UserId UserId,
    DateTime DeletedAtUtc);
