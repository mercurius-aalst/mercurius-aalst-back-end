using Mercurius.Modules.Shared;

namespace Mercurius.Modules.Identity.Contracts;

public sealed record UserProfileChangedIntegrationEvent(
    UserId UserId,
    string? Username,
    string DisplayName,
    bool IsDeleted,
    DateTime UpdatedAtUtc);
