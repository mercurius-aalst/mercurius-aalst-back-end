using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Identity.Contracts;

public sealed record UsernameChangedIntegrationEvent(
    UserId UserId,
    string Username,
    DateTime UpdatedAtUtc);
